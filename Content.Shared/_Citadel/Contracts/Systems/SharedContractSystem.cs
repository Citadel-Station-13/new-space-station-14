using Content.Shared._Citadel.Contracts.Components;

namespace Content.Shared._Citadel.Contracts.Systems;

/// <summary>
/// This handles core contract logic like state transitions.
/// </summary>
public abstract class SharedContractSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {

    }

    private void Transition(Entity<CitadelContractComponent> ent, ContractState newState)
    {
        var oldState = ent.Comp.State;
        ent.Comp.State = newState;

        var ev = new ContractStateChanged(ent, oldState, newState);
        RaiseLocalEvent(ent, ref ev, broadcast: true);
    }

    public bool TrySignContract(Entity<CitadelContractComponent> ent)
    {
        if (ent.Comp.State is not ContractStateUnsigned)
            return false;

        if (ent.Comp.PartyA.Count == 0)
            return false;

        if (ent.Comp.PartyB.Count == 0)
            return false;

        Transition(ent, new ContractStateSigned());
        return true;
    }

    public bool TryBreachContract(Entity<CitadelContractComponent> ent, Party party)
    {
        if (ent.Comp.State is not ContractStateSigned)
            return false;

        Transition(ent, new ContractStateBreached { BreachingParty = party });
        return true;
    }

    public bool TryCloseOutContract(Entity<CitadelContractComponent> ent)
    {
        if (ent.Comp.State is not ContractStateSigned)
            return false;

        Transition(ent, new ContractStateClosedOut());
        return true;
    }

    /// <remarks>
    ///     Signers should not be deleted before the contract is. As such, signers should be things like minds or factions, not player entities.
    /// </remarks>
    public bool TrySignOn(Entity<CitadelContractComponent> contract,
        EntityUid signer,
        Party party)
    {
        if (Deleted(signer))
            return false;

        switch (party)
        {
            case Party.PartyA:
                contract.Comp.PartyA.Add(signer);
                break;
            case Party.PartyB:
                contract.Comp.PartyB.Add(signer);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(party), party, null);
        }

        return true;
    }
}

public enum TransitionStatus
{
    Invalid = 0,
    Success,

}
