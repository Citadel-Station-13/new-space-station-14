using System.Linq;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
using Content.Server.Objectives.Interfaces;
using Content.Server.Roles.Jobs;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
    [DataDefinition]
    public sealed partial class RandomTraitorAliveCondition : IObjectiveCondition
    {
        private EntityUid? _target;

        public IObjectiveCondition GetAssigned(EntityUid mindId, MindComponent mind)
        {
            var entityMgr = IoCManager.Resolve<IEntityManager>();

            var traitors = entityMgr.System<TraitorRuleSystem>().GetOtherTraitorMindsAliveAndConnected(mind).ToList();

            if (traitors.Count == 0)
                return new EscapeShuttleCondition(); //You were made a traitor by admins, and are the first/only.
            return new RandomTraitorAliveCondition { _target = IoCManager.Resolve<IRobustRandom>().Pick(traitors).Id };
        }

        public string Title
        {
            get
            {
                var targetName = string.Empty;
                var ents = IoCManager.Resolve<IEntityManager>();
                var jobs = ents.System<JobSystem>();
                var jobName = jobs.MindTryGetJobName(_target);

                if (_target == null)
                    return Loc.GetString("objective-condition-other-traitor-alive-title", ("targetName", targetName), ("job", jobName));

                var minds = ents.System<MindSystem>();
                if (minds.TryGetMind(_target.Value, out _, out var mind) &&
                    mind.OwnedEntity is { Valid: true } owned)
                {
                    targetName = ents.GetComponent<MetaDataComponent>(owned).EntityName;
                }

                return Loc.GetString("objective-condition-other-traitor-alive-title", ("targetName", targetName), ("job", jobName));
            }
        }

        public string Description => Loc.GetString("objective-condition-other-traitor-alive-description");

        public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ("Objects/Misc/bureaucracy.rsi"), "folder-white");

        public float Progress
        {
            get
            {
                var entityManager = IoCManager.Resolve<EntityManager>();
                var mindSystem = entityManager.System<MindSystem>();
                return _target == null ||
                       !mindSystem.TryGetMind(_target.Value, out _, out var mind) ||
                       !mindSystem.IsCharacterDeadIc(mind)
                    ? 1f
                    : 0f;
            }
        }

        public float Difficulty => 1.75f;

        public bool Equals(IObjectiveCondition? other)
        {
            return other is RandomTraitorAliveCondition kpc && Equals(_target, kpc._target);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is RandomTraitorAliveCondition alive && alive.Equals(this);
        }

        public override int GetHashCode()
        {
            return _target?.GetHashCode() ?? 0;
        }
    }
}
