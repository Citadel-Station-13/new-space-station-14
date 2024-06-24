using Robust.Shared.GameStates;

namespace Content.Shared._Citadel.Contracts.Components;

/// <summary>
/// This is used for binding contracts between two parties (whether that be corps, players, or both.)
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CitadelContractComponent : Component
{
    /// <summary>
    ///     The first party (and author) of the contract.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntityUid> PartyA = new();

    /// <summary>
    ///     The second party (and recipient) of the contract.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntityUid> PartyB = new();

    [DataField, AutoNetworkedField]
    public ContractState State = new ContractStateUnsigned();
}

public enum Party
{
    PartyA,
    PartyB,
}

public abstract partial record ContractState();
/// <summary>
///     Contract has not yet been signed by any party, and as such is not final.
/// </summary>
[DataDefinition]
public sealed partial record ContractStateUnsigned : ContractState;

/// <summary>
///     Contract has been signed, parties are locked in.
/// </summary>
[DataDefinition]
public sealed partial record ContractStateSigned : ContractState;

/// <summary>
///     Contract terms were breached by some party, and the breaching party is to be penalized.
/// </summary>
[DataDefinition]
public sealed partial record ContractStateBreached : ContractState
{
    /// <summary>Party that breached the contract.</summary>
    [DataField]
    public Party BreachingParty { get; set; }
}

/// <summary>
///     Contract was closed out, and can no longer be breached as rewards have been paid out and it is finalized.
/// </summary>
[DataDefinition]
public sealed partial record ContractStateClosedOut : ContractState;

[ByRefEvent]
public record struct ContractStateChanged(Entity<CitadelContractComponent> Contract, ContractState Old, ContractState New);
