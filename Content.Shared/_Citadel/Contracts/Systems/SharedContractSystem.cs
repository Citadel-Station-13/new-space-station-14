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

    public TransitionError? TrySignContract(Entity<CitadelContractComponent> ent)
    {
        if (ent.Comp.State is not ContractStateUnsigned)
            return new TECannotTransitionFrom(ent.Comp.State);

        if (ent.Comp.PartyA.Count == 0)
            return new TENoPartyA();

        if (ent.Comp.PartyB.Count == 0)
            return new TENoPartyB();

        Transition(ent, new ContractStateSigned());
        return null;
    }

    public TransitionError? TryBreachContract(Entity<CitadelContractComponent> ent, Party party)
    {
        if (ent.Comp.State is not ContractStateSigned)
            return new TECannotTransitionFrom(ent.Comp.State);

        Transition(ent, new ContractStateBreached { BreachingParty = party });
        return null;
    }

    public TransitionError? TryCloseOutContract(Entity<CitadelContractComponent> ent)
    {
        if (ent.Comp.State is not ContractStateSigned)
            return new TECannotTransitionFrom(ent.Comp.State);

        Transition(ent, new ContractStateClosedOut());
        return null;
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
    CannotTransitionFrom,
    NoPartyA,
    NoPartyB,
}

public abstract record TransitionError();
public sealed record TENoPartyA : TransitionError;
public sealed record TENoPartyB : TransitionError;
public sealed record TECannotTransitionFrom(ContractState State) : TransitionError;
