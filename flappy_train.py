import os
import random
import numpy as np
from collections import deque
from datetime import datetime

import torch
import torch.nn as nn
import torch.optim as optim
import torch.nn.functional as F
from torch.utils.tensorboard import SummaryWriter


# ======================================================
# Modelo Double DQN
# ======================================================
class DQN(nn.Module):
    def __init__(self, input_dim=4, output_dim=2):
        super(DQN, self).__init__()
        self.fc1 = nn.Linear(input_dim, 64)
        self.fc2 = nn.Linear(64, 64)
        self.fc3 = nn.Linear(64, output_dim)

    def forward(self, x):
        x = F.relu(self.fc1(x))
        x = F.relu(self.fc2(x))
        return self.fc3(x)


# ======================================================
# Agente com Double DQN + Checkpoint
# ======================================================
class DoubleDQNAgent:
    def __init__(self, state_size=4, action_size=2, lr=1e-3, gamma=0.99,
                 epsilon_start=1.0, epsilon_end=0.01, epsilon_decay=0.99,
                 memory_size=50000, batch_size=64, target_update_freq=50,
                 checkpoint_dir="checkpoints"):

        self.state_size = state_size
        self.action_size = action_size
        self.gamma = gamma
        self.batch_size = batch_size
        self.epsilon = epsilon_start
        self.epsilon_min = epsilon_end
        self.epsilon_decay = epsilon_decay
        self.target_update_freq = target_update_freq

        self.memory = deque(maxlen=memory_size)
        self.device = torch.device("cuda" if torch.cuda.is_available() else "cpu")

        self.model = DQN(state_size, action_size).to(self.device)
        self.target_model = DQN(state_size, action_size).to(self.device)
        self.optimizer = optim.Adam(self.model.parameters(), lr=lr)
        self.update_target_model()

        self.checkpoint_dir = checkpoint_dir
        os.makedirs(self.checkpoint_dir, exist_ok=True)

        log_dir = os.path.join("runs", "flappybird_dqn_" + datetime.now().strftime("%Y%m%d-%H%M%S"))
        self.writer = SummaryWriter(log_dir)
        self.step_count = 0

    def update_target_model(self):
        self.target_model.load_state_dict(self.model.state_dict())

    def remember(self, state, action, reward, next_state, done):
        self.memory.append((state, action, reward, next_state, done))

    def act(self, state):
        if np.random.rand() <= self.epsilon:
            return random.randrange(self.action_size)
        state = torch.FloatTensor(state).unsqueeze(0).to(self.device)
        with torch.no_grad():
            q_values = self.model(state)
        return torch.argmax(q_values).item()

    def replay(self):
        if len(self.memory) < self.batch_size:
            return None

        minibatch = random.sample(self.memory, self.batch_size)
        states, actions, rewards, next_states, dones = zip(*minibatch)

        states = torch.FloatTensor(states).to(self.device)
        actions = torch.LongTensor(actions).unsqueeze(1).to(self.device)
        rewards = torch.FloatTensor(rewards).to(self.device)
        next_states = torch.FloatTensor(next_states).to(self.device)
        dones = torch.FloatTensor(dones).to(self.device)

        q_values = self.model(states).gather(1, actions).squeeze(1)

        with torch.no_grad():
            next_actions = self.model(next_states).argmax(1).unsqueeze(1)
            next_q_values = self.target_model(next_states).gather(1, next_actions).squeeze(1)
            target_q = rewards + (1 - dones) * self.gamma * next_q_values

        loss = F.mse_loss(q_values, target_q)

        self.optimizer.zero_grad()
        loss.backward()
        self.optimizer.step()

        if self.epsilon > self.epsilon_min:
            self.epsilon *= self.epsilon_decay

        self.step_count += 1
        self.writer.add_scalar("Loss/train", loss.item(), self.step_count)
        self.writer.add_scalar("Epsilon", self.epsilon, self.step_count)

        return loss.item()

    # ======================================================
    # Checkpoint Save/Load
    # ======================================================
    # --- save_checkpoint ---
    def save_checkpoint(self, episode):
        checkpoint_path = os.path.join(self.checkpoint_dir, f"checkpoint_{episode}.pth")
        torch.save({
            "episode": episode,
            "model_state_dict": self.model.state_dict(),
            "target_state_dict": self.target_model.state_dict(),
            "optimizer_state_dict": self.optimizer.state_dict(),
            "epsilon": self.epsilon,
            "memory": list(self.memory),  # salva o buffer de replay
        }, checkpoint_path)
        print(f"[Checkpoint] Saved at {checkpoint_path}")

    # --- load_checkpoint ---
    def load_checkpoint(self, path):
        checkpoint = torch.load(path, map_location=self.device)
        self.model.load_state_dict(checkpoint["model_state_dict"])
        self.target_model.load_state_dict(checkpoint["target_state_dict"])
        self.optimizer.load_state_dict(checkpoint["optimizer_state_dict"])
        self.epsilon = checkpoint["epsilon"]
        if "memory" in checkpoint:
            self.memory = deque(checkpoint["memory"], maxlen=50000)
            print(f"[Checkpoint] Replay buffer restaurado com {len(self.memory)} experiências")
        print(f"[Checkpoint] Loaded from {path}, starting from episode {checkpoint['episode']}")
        return checkpoint["episode"]

# ======================================================
# Treinamento principal
# ======================================================
def train_flappy(env, episodes=1000, max_steps=1000000, resume_path=None, checkpoint_interval=50):
    agent = DoubleDQNAgent()
    start_episode = 0

    # Retomar de checkpoint (se fornecido)
    if resume_path is not None and os.path.exists(resume_path):
        start_episode = agent.load_checkpoint(resume_path)

    for e in range(start_episode, episodes):
        state = env.reset()
        done = False
        total_reward = 0
        losses = []

        for _ in range(max_steps):
            action = agent.act(state)
            next_state, reward, done, _ = env.step(action)
            agent.remember(state, action, reward, next_state, done)
            state = next_state
            total_reward += reward

            loss = agent.replay()
            if loss:
                losses.append(loss)

            if done:
                break

        if e % agent.target_update_freq == 0:
            agent.update_target_model()

        avg_loss = np.mean(losses) if losses else 0
        agent.writer.add_scalar("Reward/episode", total_reward, e)
        agent.writer.add_scalar("Loss/episode", avg_loss, e)

        print(f"Episode {e+1}/{episodes} | Reward: {total_reward:.2f} | Loss: {avg_loss:.4f} | Epsilon: {agent.epsilon:.3f}")

        # Checkpoint periódico
        if (e + 1) % checkpoint_interval == 0:
            agent.save_checkpoint(e + 1)

    agent.save_checkpoint(episodes)
    agent.writer.close()
    print("✅ Treinamento finalizado e modelo salvo.")


# ======================================================
# Integração com Build do Jogo (Unity ML-Agents)
# ======================================================
if __name__ == "__main__":
    from mlagents_envs.environment import UnityEnvironment
    from mlagents_envs.base_env import ActionTuple

    env_path = "Build\Flappy Bird.exe"

    env = UnityEnvironment(file_name=env_path, seed=1, side_channels=[])
    env.reset()

    behavior_name = list(env.behavior_specs)[0]
    spec = env.behavior_specs[behavior_name]

    def reset_env():
        env.reset()
        decision_steps, _ = env.get_steps(behavior_name)
        return decision_steps.obs[0][0]

    def step_env(action):
        action_tuple = ActionTuple()
        action_tuple.add_discrete(np.array([[action]]))
        env.set_actions(behavior_name, action_tuple)
        env.step()
        decision_steps, terminal_steps = env.get_steps(behavior_name)

        if len(terminal_steps) > 0:
            obs = terminal_steps.obs[0][0]
            reward = terminal_steps.reward[0]
            done = True
        else:
            obs = decision_steps.obs[0][0]
            reward = decision_steps.reward[0]
            done = False
        return obs, reward, done, {}

    class FlappyUnityWrapper:
        def reset(self):
            return reset_env()

        def step(self, action):
            return step_env(action)

    flappy_env = FlappyUnityWrapper()

    train_flappy(flappy_env, episodes=2000)