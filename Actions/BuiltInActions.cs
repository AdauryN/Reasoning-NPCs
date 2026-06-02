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
    
}
