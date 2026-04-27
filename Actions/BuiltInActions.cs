using System.Threading.Tasks;
using NPC_AI.Brain;
using NPC_AI.Core;
using UnityEngine;

namespace NPC_AI.Actions
{
    // All built-in action implementations live here.
    // Each class is intentionally small — game-specific logic belongs in subclasses of NPCController.

    public class AttackMeleeAction : INPCAction
    {
        public string ActionType => "ATTACK_MELEE";
        public string PromptDescription => "Move toward the target and attack with a melee weapon.";
        public bool CanExecute(NPCWorldView v) => v.DistanceToPlayer < 15f;
        public Task ExecuteAsync(ActionCommand cmd, NPCController npc)
        {
            Debug.Log($"[Action] {npc.NpcId}: ATTACK_MELEE → {cmd.Target}");
            return Task.CompletedTask;
        }
    }

    public class ChargeAction : INPCAction
    {
        public string ActionType => "CHARGE";
        public string PromptDescription => "Sprint directly at the player for a powerful opening strike.";
        public bool CanExecute(NPCWorldView v) => v.DistanceToPlayer > 3f;
        public Task ExecuteAsync(ActionCommand cmd, NPCController npc)
        {
            Debug.Log($"[Action] {npc.NpcId}: CHARGE → {cmd.Target}");
            return Task.CompletedTask;
        }
    }

    public class DodgeAction : INPCAction
    {
        public string ActionType => "DODGE";
        public string PromptDescription => "Dodge sideways to evade an incoming attack.";
        public bool CanExecute(NPCWorldView v) => true;
        public Task ExecuteAsync(ActionCommand cmd, NPCController npc)
        {
            Debug.Log($"[Action] {npc.NpcId}: DODGE");
            return Task.CompletedTask;
        }
    }

    public class FleeAction : INPCAction
    {
        public string ActionType => "RETREAT";
        public string PromptDescription => "Disengage and move away from the threat to recover or regroup.";
        public bool CanExecute(NPCWorldView v) => true;
        public Task ExecuteAsync(ActionCommand cmd, NPCController npc)
        {
            Debug.Log($"[Action] {npc.NpcId}: RETREAT");
            return Task.CompletedTask;
        }
    }

    public class TauntAction : INPCAction
    {
        public string ActionType => "TAUNT";
        public string PromptDescription => "Taunt the player to provoke an emotional reaction or bait an attack.";
        public bool CanExecute(NPCWorldView v) => true;
        public Task ExecuteAsync(ActionCommand cmd, NPCController npc)
        {
            Debug.Log($"[Action] {npc.NpcId}: TAUNT → {cmd.Target}");
            return Task.CompletedTask;
        }
    }

    public class PatrolAction : INPCAction
    {
        public string ActionType => "PATROL";
        public string PromptDescription => "Resume patrol route while keeping watch for threats.";
        public bool CanExecute(NPCWorldView v) => true;
        public Task ExecuteAsync(ActionCommand cmd, NPCController npc)
        {
            Debug.Log($"[Action] {npc.NpcId}: PATROL");
            return Task.CompletedTask;
        }
    }

    public class IdleAction : INPCAction
    {
        public string ActionType => "IDLE";
        public string PromptDescription => "Hold position and observe. Useful when waiting for the player to act.";
        public bool CanExecute(NPCWorldView v) => true;
        public Task ExecuteAsync(ActionCommand cmd, NPCController npc)
        {
            Debug.Log($"[Action] {npc.NpcId}: IDLE");
            return Task.CompletedTask;
        }
    }

    public class ShootArrowAction : INPCAction
    {
        public string ActionType => "SHOOT_ARROW";
        public string PromptDescription => "Fire a ranged arrow at the target. Requires line of sight.";
        public bool CanExecute(NPCWorldView v) => v.DistanceToPlayer > 2f;
        public Task ExecuteAsync(ActionCommand cmd, NPCController npc)
        {
            Debug.Log($"[Action] {npc.NpcId}: SHOOT_ARROW → {cmd.Target}");
            return Task.CompletedTask;
        }
    }

    public class MoveToCoverAction : INPCAction
    {
        public string ActionType => "MOVE_TO_COVER";
        public string PromptDescription => "Reposition to nearby cover to reduce exposure to attacks.";
        public bool CanExecute(NPCWorldView v) => true;
        public Task ExecuteAsync(ActionCommand cmd, NPCController npc)
        {
            Debug.Log($"[Action] {npc.NpcId}: MOVE_TO_COVER");
            return Task.CompletedTask;
        }
    }

    public class CleaveAction : INPCAction
    {
        public string ActionType => "CLEAVE";
        public string PromptDescription => "Wide swing that hits multiple nearby enemies.";
        public bool CanExecute(NPCWorldView v) => v.DistanceToPlayer < 5f;
        public Task ExecuteAsync(ActionCommand cmd, NPCController npc)
        {
            Debug.Log($"[Action] {npc.NpcId}: CLEAVE");
            return Task.CompletedTask;
        }
    }
}
