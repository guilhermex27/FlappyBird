#!/usr/bin/env python3
"""
Train Dueling Double DQN (DDQL) against a Unity executable with ML-Agents.

Assumptions:
- Unity Behavior name is the first behavior exposed (script will auto-select it).
- Observations vector length == 6 (birdX, birdY, pipeX, pipeY, gravity, jumpForce).
- Discrete action branch with size 3: [0: nothing, 1: jump, 2: parachute].
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

        # value stream
        self.value_fc = nn.Linear(hidden, 64)
        self.value_out = nn.Linear(64, 1)

        # advantage stream
        self.adv_fc = nn.Linear(hidden, 64)
        self.adv_out = nn.Linear(64, action_dim)

        self.relu = nn.ReLU()

    def forward(self, x):
        x = self.relu(self.fc1(x))
        x = self.relu(self.fc2(x))

        v = self.relu(self.value_fc(x))
        v = self.value_out(v)                     # (batch, 1)

        a = self.relu(self.adv_fc(x))
        a = self.adv_out(a)                       # (batch, action_dim)

        # combine: Q(s,a) = V(s) + (A(s,a) - mean_a(A(s,a)))
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
    def __init__(
        self,
        state_dim,
        action_dim,
        device,
        lr=1e-4,
        gamma=0.99,
        batch_size=64,
        buffer_capacity=50000,
        tau=1.0,
    ):
        self.device = device
        self.q_net = DuelingDQN(state_dim, action_dim).to(device)
        self.target_net = DuelingDQN(state_dim, action_dim).to(device)
        self.target_net.load_state_dict(self.q_net.state_dict())
        self.optimizer = optim.Adam(self.q_net.parameters(), lr=lr)
        self.gamma = gamma
        self.batch_size = batch_size
        self.buffer = ReplayBuffer(buffer_capacity)
        self.action_dim = action_dim
        self.tau = tau  # for soft update if desired (tau=1 => hard copy)

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

    def soft_update_target(self):
        for target_param, param in zip(self.target_net.parameters(), self.q_net.parameters()):
            target_param.data.copy_(self.tau * param.data + (1.0 - self.tau) * target_param.data)

    def train_step(self):
        if len(self.buffer) < self.batch_size:
            return None

        s, a, r, s2, d = self.buffer.sample(self.batch_size)

        s_t = torch.tensor(s, device=self.device)
        a_t = torch.tensor(a, device=self.device).unsqueeze(1)
        r_t = torch.tensor(r, device=self.device).unsqueeze(1)
        s2_t = torch.tensor(s2, device=self.device)
        d_t = torch.tensor(d, device=self.device).unsqueeze(1)

        # current Q(s,a)
        q_values = self.q_net(s_t).gather(1, a_t)

        # Double DQN: action selection by online network, evaluation by target network
        next_actions = torch.argmax(self.q_net(s2_t), dim=1).unsqueeze(1)  # (B,1)
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
def train(
    env_path,
    run_id,
    max_episodes=2000,
    max_steps_per_episode=2000,
    save_dir="checkpoints",
    checkpoint_interval=50,
    gamma=0.99,
    lr=1e-4,
    batch_size=64,
    buffer_capacity=50000,
    eps_start=1.0,
    eps_end=0.05,
    eps_decay=20000,
    target_update_freq=5,  # episodes
    device_name=None,
):
    device = torch.device(device_name if device_name else ("cuda" if torch.cuda.is_available() else "cpu"))
    print(f"Using device: {device}")

    # Launch Unity environment
    env = UnityEnvironment(file_name=env_path, no_graphics=True)
    env.reset()

    # pick the first behavior
    behavior_name = list(env.behavior_specs.keys())[0]
    spec = env.behavior_specs[behavior_name]

    # Sanity: print observation shapes
    print(f"Selected behavior: {behavior_name}")
    obs_shapes = [o.shape for o in spec.observation_shapes]
    print("Observation shapes:", obs_shapes)
    # assume vector observation first and length 6
    state_dim = obs_shapes[0][1] if len(obs_shapes) and len(obs_shapes[0]) > 1 else obs_shapes[0][0]
    action_dim = spec.action_spec.discrete_branches[0]  # branch size (3)

    print(f"State dim: {state_dim}, Action dim: {action_dim}")

    agent = DDQLAgent(
        state_dim=state_dim,
        action_dim=action_dim,
        device=device,
        lr=lr,
        gamma=gamma,
        batch_size=batch_size,
        buffer_capacity=buffer_capacity,
    )

    # Logging
    timestamp = datetime.now().strftime("%Y%m%d-%H%M%S")
    logdir = os.path.join("runs", f"{run_id}_{timestamp}")
    writer = SummaryWriter(logdir)

    os.makedirs(save_dir, exist_ok=True)

    global_step = 0
    eps = eps_start

    for ep in range(1, max_episodes + 1):
        env.reset()
        decision_steps, terminal_steps = env.get_steps(behavior_name)
        # pick first agent in decision_steps
        if len(decision_steps) == 0:
            # no agent ready, step once
            env.step()
            decision_steps, terminal_steps = env.get_steps(behavior_name)

        agent_id = list(decision_steps.agent_id_to_index.keys())[0]
        done = False
        ep_reward = 0.0
        ep_losses = []
        step = 0

        while not done and step < max_steps_per_episode:
            # Get current observation for agent_id
            decision_steps, terminal_steps = env.get_steps(behavior_name)
            if agent_id in terminal_steps:
                # episode ended at environment side
                obs = terminal_steps[agent_id].obs[0]
                reward = terminal_steps[agent_id].reward
                done = True
                next_obs = obs
                # note: terminal_steps sometimes contain final obs
            elif agent_id in decision_steps:
                obs = decision_steps[agent_id].obs[0]
                reward = decision_steps[agent_id].reward
                next_obs = None
            else:
                # No info about the agent yet, step environment
                env.step()
                continue

            # Flatten observation vector (if needed)
            state = np.array(obs).reshape(-1)

            # Epsilon decay (linear / exponential-style)
            eps = max(eps_end, eps_start - (global_step / eps_decay) * (eps_start - eps_end))

            # select action
            action = agent.act(state, eps)

            # send action to environment (discrete)
            action_tuple = ActionTuple(discrete=np.array([[action]], dtype=np.int32))
            env.set_actions(behavior_name, action_tuple)
            env.step()
            global_step += 1
            step += 1

            # After step, gather next decision/terminal steps to observe reward and next state
            decision_steps, terminal_steps = env.get_steps(behavior_name)
            if agent_id in terminal_steps:
                ns_obs = terminal_steps[agent_id].obs[0]
                r = terminal_steps[agent_id].reward
                done = True
                next_state = np.array(ns_obs).reshape(-1)
            elif agent_id in decision_steps:
                ns_obs = decision_steps[agent_id].obs[0]
                r = decision_steps[agent_id].reward
                next_state = np.array(ns_obs).reshape(-1)
            else:
                # fallback
                r = 0.0
                next_state = state
            ep_reward += float(r)

            # memorize and train
            agent.memorize(state, action, float(r), next_state, done)
            loss = agent.train_step()
            if loss is not None:
                ep_losses.append(loss)
                writer.add_scalar("loss/train_step", loss, global_step)

        # end episode
        # target update every N episodes (hard copy)
        if ep % target_update_freq == 0:
            agent.update_target()

        avg_loss = float(np.mean(ep_losses)) if ep_losses else 0.0
        print(f"Episode {ep:04d} | Reward {ep_reward:.2f} | AvgLoss {avg_loss:.6f} | Eps {eps:.4f}")

        # logging
        writer.add_scalar("episode/reward", ep_reward, ep)
        writer.add_scalar("episode/avg_loss", avg_loss, ep)
        writer.add_scalar("training/eps", eps, ep)

        # checkpointing
        if ep % checkpoint_interval == 0 or ep == max_episodes:
            ckpt_path = os.path.join(save_dir, f"{run_id}_ep{ep}.pth")
            torch.save(
                {
                    "episode": ep,
                    "q_state_dict": agent.q_net.state_dict(),
                    "target_state_dict": agent.target_net.state_dict(),
                    "optimizer_state_dict": agent.optimizer.state_dict(),
                    "eps": eps,
                },
                ckpt_path,
            )
            print(f"Saved checkpoint: {ckpt_path}")

    # final save
    final_path = os.path.join(save_dir, f"{run_id}_final.pth")
    torch.save({"q_state_dict": agent.q_net.state_dict()}, final_path)
    print(f"Training finished. Final model saved at {final_path}")

    writer.close()
    env.close()


# --------------------------
# CLI
# --------------------------
if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--env", type=str, required=True, help="Path to Unity executable (.exe) or empty for Editor")
    parser.add_argument("--run-id", type=str, default="flappy_ddql", help="run id for logs & checkpoints")
    parser.add_argument("--max-episodes", type=int, default=2000)
    parser.add_argument("--checkpoint-interval", type=int, default=50)
    parser.add_argument("--save-dir", type=str, default="checkpoints")
    parser.add_argument("--batch-size", type=int, default=64)
    parser.add_argument("--buffer-capacity", type=int, default=50000)
    parser.add_argument("--eps-start", type=float, default=1.0)
    parser.add_argument("--eps-end", type=float, default=0.05)
    parser.add_argument("--eps-decay", type=float, default=20000)
    parser.add_argument("--device", type=str, default=None)
    args = parser.parse_args()

    train(
        env_path=args.env,
        run_id=args.run_id,
        max_episodes=args.max_episodes,
        checkpoint_interval=args.checkpoint_interval,
        save_dir=args.save_dir,
        batch_size=args.batch_size,
        buffer_capacity=args.buffer_capacity,
        eps_start=args.eps_start,
        eps_end=args.eps_end,
        eps_decay=args.eps_decay,
        device_name=args.device,
    )