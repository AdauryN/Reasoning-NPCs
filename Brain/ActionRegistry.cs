using System.Collections.Generic;
using NPC_AI.Actions;
using UnityEngine;

namespace NPC_AI.Brain
{
    /// Maps action type strings to INPCAction implementations.
    /// Populated at startup with all built-in actions.
    /// Game-specific actions can be registered via Register().
    public class ActionRegistry
    {
        private readonly Dictionary<string, INPCAction> _actions =
            new Dictionary<string, INPCAction>(System.StringComparer.OrdinalIgnoreCase);

        public ActionRegistry()
        {
            RegisterDefaults();
        }

        public void Register(INPCAction action)
        {
            if (_actions.ContainsKey(action.ActionType))
                Debug.LogWarning($"[ActionRegistry] Overwriting action: {action.ActionType}");
            _actions[action.ActionType] = action;
        }

        public INPCAction Get(string actionType) =>
            _actions.TryGetValue(actionType, out var action) ? action : null;

        public bool Contains(string actionType) => _actions.ContainsKey(actionType);

        private void RegisterDefaults()
        {
            Register(new AttackMeleeAction());
            Register(new ChargeAction());
            Register(new DodgeAction());
            Register(new FleeAction());
            Register(new TauntAction());
            Register(new PatrolAction());
            Register(new IdleAction());
            Register(new ShootArrowAction());
            Register(new MoveToCoverAction());
            Register(new CleaveAction());
        }
    }
}
