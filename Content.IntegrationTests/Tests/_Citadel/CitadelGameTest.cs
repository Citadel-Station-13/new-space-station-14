#nullable enable
using System.Linq;
using System.Reflection;
using Content.IntegrationTests.Pair;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests._Citadel;

public abstract class CitadelGameTest
{
    private bool _pairDirty = false;

    protected TestPair Pair = default!; // NULLABILITY: This is always set during test setup.
    protected RobustIntegrationTest.ServerIntegrationInstance Server => Pair.Server;
    protected RobustIntegrationTest.ClientIntegrationInstance Client => Pair.Client;
    protected ICommonSession? Player => Pair.Player;

    protected IEntityManager SEntMan => Server.EntMan;

    protected void DirtyClientServerPair()
    {
        _pairDirty = true;
    }

    [SetUp]
    public virtual async Task Setup()
    {
        _pairDirty = false;
        Pair = await PoolManager.GetServerClient(new PoolSettings {Connected = true});

        foreach (var field in GetType().GetAllFields())
        {
            if (field.GetCustomAttribute<SystemAttribute>() is {} sysAttrib)
            {
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (sysAttrib.Side is Side.Server)
                {
                    field.SetValue(this, Server.EntMan.EntitySysManager.GetEntitySystem(field.FieldType));
                }
                else
                {
                    field.SetValue(this, Client.EntMan.EntitySysManager.GetEntitySystem(field.FieldType));
                }
            }
            else if (field.GetCustomAttribute<SidedDependencyAttribute>() is { } depAttrib)
            {
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (depAttrib.Side is Side.Server)
                {
                    field.SetValue(this, Server.InstanceDependencyCollection.ResolveType(field.FieldType));
                }
                else
                {
                    field.SetValue(this, Client.InstanceDependencyCollection.ResolveType(field.FieldType));
                }
            }
        }
    }

    [TearDown]
    public virtual async Task TearDown()
    {
        if (!_pairDirty)
            await Pair.CleanReturnAsync();
        else
            await Pair.DisposeAsync();
    }

    protected EntityUid ToClientUid(EntityUid serverUid)
    {
        return Pair.ToClientUid(serverUid);
    }

    protected EntityUid ToServerUid(EntityUid clientUid)
    {
        return Pair.ToServerUid(clientUid);
    }

    protected T GetSysServer<T>()
        where T : EntitySystem
    {
        return Server.EntMan.System<T>();
    }

    protected T GetSysClient<T>()
        where T : EntitySystem
    {
        return Client.EntMan.System<T>();
    }

    protected T SComp<T>(EntityUid target)
        where T : IComponent
    {
        return SEntMan.GetComponent<T>(target);
    }

    protected Entity<T> SEntity<T>(EntityUid target)
        where T : IComponent
    {
        return new(target, SEntMan.GetComponent<T>(target));
    }

    protected EntityUid Spawn(string id)
    {
        return SEntMan.Spawn(id);
    }
}
