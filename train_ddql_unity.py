#!/usr/bin/env python3
"""
Train Dueling Double DQN (DDQL) against a Unity executable with ML-Agents.
"""

import argparse
import os
import random
from collections import deque
from datetime import datetime

import numpy as np
import torch
import torch.nn as nn
import torch.optim as optim
from torch.utils.tensorboard import SummaryWriter

from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.base_env import ActionTuple

# --------------------------
# Dueling DQN architecture
# --------------------------
class DuelingDQN(nn.Module):
    def __init__(self, state_dim: int, action_dim: int, hidden=128):
        super().__init__()
        self.fc1 = nn.Linear(state_dim, hidden)
        self.fc2 = nn.Linear(hidden, hidden)
        self.value_fc = nn.Linear(hidden, 64)
        self.value_out = nn.Linear(64, 1)
        self.adv_fc = nn.Linear(hidden, 64)
        self.adv_out = nn.Linear(64, action_dim)
        self.relu = nn.ReLU()

    def forward(self, x):
        x = self.relu(self.fc1(x))
        x = self.relu(self.fc2(x))
        v = self.relu(self.value_fc(x))
        v = self.value_out(v)
        a = self.relu(self.adv_fc(x))
        a = self.adv_out(a)
        q = v + (a - a.mean(dim=1, keepdim=True))
        return q

# --------------------------
# Replay Buffer
# --------------------------
class ReplayBuffer:
    def __init__(self, capacity: int):
        self.buffer = deque(maxlen=capacity)

    def push(self, state, action, reward, next_state, done):
        self.buffer.append((state, action, reward, next_state, float(done)))

    def sample(self, batch_size: int):
        batch = random.sample(self.buffer, batch_size)
        states, actions, rewards, next_states, dones = zip(*batch)
        return (
            np.array(states, dtype=np.float32),
            np.array(actions, dtype=np.int64),
            np.array(rewards, dtype=np.float32),
            np.array(next_states, dtype=np.float32),
            np.array(dones, dtype=np.float32),
        )

    def __len__(self):
        return len(self.buffer)

# --------------------------
# Agent (DDQL)
# --------------------------
class DDQLAgent:
    def __init__(self, state_dim, action_dim, device, lr=1e-4, gamma=0.99, batch_size=64, buffer_capacity=50000, tau=1.0):
        self.device = device
        self.q_net = DuelingDQN(state_dim, action_dim).to(device)
        self.target_net = DuelingDQN(state_dim, action_dim).to(device)
        self.target_net.load_state_dict(self.q_net.state_dict())
        self.optimizer = optim.Adam(self.q_net.parameters(), lr=lr)
        self.gamma = gamma
        self.batch_size = batch_size
        self.buffer = ReplayBuffer(buffer_capacity)
        self.action_dim = action_dim
        self.tau = tau

    def act(self, state: np.ndarray, eps: float):
        if random.random() < eps:
            return random.randrange(self.action_dim)
        state_t = torch.from_numpy(state.astype(np.float32)).unsqueeze(0).to(self.device)
        with torch.no_grad():
            qvals = self.q_net(state_t)
        return int(torch.argmax(qvals, dim=1).item())

    def memorize(self, s, a, r, s2, done):
        self.buffer.push(s, a, r, s2, done)

    def update_target(self):
        self.target_net.load_state_dict(self.q_net.state_dict())

    def train_step(self):
        if len(self.buffer) < self.batch_size:
            return None
        s, a, r, s2, d = self.buffer.sample(self.batch_size)
        s_t, a_t, r_t, s2_t, d_t = (
            torch.tensor(s, device=self.device),
            torch.tensor(a, device=self.device).unsqueeze(1),
            torch.tensor(r, device=self.device).unsqueeze(1),
            torch.tensor(s2, device=self.device),
            torch.tensor(d, device=self.device).unsqueeze(1),
        )
        q_values = self.q_net(s_t).gather(1, a_t)
        next_actions = torch.argmax(self.q_net(s2_t), dim=1).unsqueeze(1)
        next_q = self.target_net(s2_t).gather(1, next_actions)
        target_q = r_t + (1.0 - d_t) * self.gamma * next_q
        loss = nn.MSELoss()(q_values, target_q.detach())
        self.optimizer.zero_grad()
        loss.backward()
        self.optimizer.step()
        return loss.item()

# --------------------------
# Training loop with Unity
# --------------------------
def train(env_path, run_id, max_episodes=5000, max_steps_per_episode=5000, save_dir="checkpoints", checkpoint_interval=100, gamma=0.99, lr=1e-4, batch_size=1024, buffer_capacity=100000, eps_start=1.0, eps_end=0.05, eps_decay=500000, target_update_freq=5, device_name=None):
    device = torch.device(device_name if device_name else ("cuda" if torch.cuda.is_available() else "cpu"))
    print(f"Using device: {device}")
    
    env = UnityEnvironment(file_name=env_path, no_graphics=False if env_path else False)
    env.reset()

    behavior_name = list(env.behavior_specs.keys())[0]
    spec = env.behavior_specs[behavior_name]
    print(f"Selected behavior: {behavior_name}")
    
    # <<< CORREÇÃO PRINCIPAL PARA A SUA VERSÃO DO ML-AGENTS >>>
    vector_obs_spec = spec.observation_specs[0]
    state_dim = vector_obs_spec.shape[0] # Pega o tamanho da observação (será 10)
    action_dim = spec.action_spec.discrete_branches[0]
    print(f"State dim: {state_dim}, Action dim: {action_dim}")

    agent = DDQLAgent(state_dim=state_dim, action_dim=action_dim, device=device, lr=lr, gamma=gamma, batch_size=batch_size, buffer_capacity=buffer_capacity)
    
    timestamp = datetime.now().strftime("%Y%m%d-%H%M%S")
    logdir = os.path.join("runs", f"{run_id}_{timestamp}")
    writer = SummaryWriter(logdir)
    os.makedirs(save_dir, exist_ok=True)
    
    global_step = 0
    eps = eps_start

    for ep in range(1, max_episodes + 1):
        env.reset()
        decision_steps, terminal_steps = env.get_steps(behavior_name)
        
        # Lida com o caso de não haver agentes prontos no primeiro passo
        if not decision_steps:
            env.step()
            decision_steps, terminal_steps = env.get_steps(behavior_name)
        
        agent_id = next(iter(decision_steps))
        done = False
        ep_reward = 0.0
        ep_losses = []

        while not done:
            decision_steps, terminal_steps = env.get_steps(behavior_name)
            
            if agent_id in terminal_steps: # Agente morreu
                obs = terminal_steps[agent_id].obs[0]
                r = terminal_steps[agent_id].reward
                done = True
                state = np.array(obs).reshape(-1)
                next_state = state
            elif agent_id in decision_steps: # Agente precisa de uma ação
                obs = decision_steps[agent_id].obs[0]
                r = decision_steps[agent_id].reward
                state = np.array(obs).reshape(-1)
            else: # Agente não está pronto, espere
                env.step()
                continue
            
            ep_reward += float(r)
            
            eps = max(eps_end, eps_start - (global_step / eps_decay) * (eps_start - eps_end))
            action = agent.act(state, eps)

            action_tuple = ActionTuple(discrete=np.array([[action]], dtype=np.int32))
            env.set_actions(behavior_name, action_tuple)
            env.step()
            global_step += 1

            # Pega o resultado da ação
            decision_steps, terminal_steps = env.get_steps(behavior_name)
            if agent_id in terminal_steps:
                next_obs = terminal_steps[agent_id].obs[0]
                reward_after_action = terminal_steps[agent_id].reward
                done = True
                next_state = np.array(next_obs).reshape(-1)
            else: # O agente ainda está vivo
                next_obs = decision_steps[agent_id].obs[0]
                reward_after_action = decision_steps[agent_id].reward
                next_state = np.array(next_obs).reshape(-1)
            
            agent.memorize(state, action, float(reward_after_action), next_state, done)
            loss = agent.train_step()
            if loss:
                ep_losses.append(loss)

        if ep % target_update_freq == 0:
            agent.update_target()

        avg_loss = float(np.mean(ep_losses)) if ep_losses else 0.0
        print(f"Episode {ep:04d} | Reward {ep_reward:.2f} | AvgLoss {avg_loss:.6f} | Eps {eps:.4f}")
        
        writer.add_scalar("episode/reward", ep_reward, ep)
        writer.add_scalar("episode/avg_loss", avg_loss, ep)
        writer.add_scalar("training/eps", eps, global_step)

        if ep % checkpoint_interval == 0:
            torch.save(agent.q_net.state_dict(), os.path.join(save_dir, f"{run_id}_ep{ep}.pth"))

    writer.close()
    env.close()
    print("Training finished.")

# --------------------------
# CLI
# --------------------------
if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--env", type=str, default=None, help="Path to Unity executable (.exe). Leave empty for Editor.")
    parser.add_argument("--run-id", type=str, default="flappy_ddql", help="Run ID for logs & checkpoints")
    parser.add_argument("--max-episodes", type=int, default=5000)
    args = parser.parse_args()
    train(env_path=args.env, run_id=args.run_id, max_episodes=args.max_episodes)