#nullable enable
using Content.Server._Citadel.Contracts;
using Content.Shared._Citadel.Contracts.Components;
using Content.Shared._Citadel.Contracts.Systems;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Citadel.Contracts;

[TestFixture]
public sealed class ContractsTest : CitadelGameTest
{
    private const string TestContractId = "TESTS_CitadelTestContract";
    private const string TestSignerId = "ToyAmongPequeno"; //Suspicious contractors.

    [TestPrototypes]
    public const string Prototypes = $"""
        - type: entity
          id: {TestContractId}
          components:
            - type: CitadelContract
        """;

    [System(Side.Server)]
    private readonly ContractSystem _sharedContractSys = default!;

    [Test]
    public void Transitions()
    {
        var contract = SEntity<CitadelContractComponent>(Spawn(TestContractId));

        // Nobody has signed on, shouldn't be able to sign it.
        Assert.That(!_sharedContractSys.TrySignContract(contract));
        Assert.That(contract.Comp.State is ContractStateUnsigned);

        // Should sign on fine.
        Assert.That(_sharedContractSys.TrySignOn(contract, Spawn(TestSignerId), Party.PartyA));
        Assert.That(_sharedContractSys.TrySignOn(contract, Spawn(TestSignerId), Party.PartyB));

        // And contract should be signable now.
        Assert.That(_sharedContractSys.TrySignContract(contract));
        Assert.That(contract.Comp.State is ContractStateSigned);

        // Now we breach it, and blame party B. Poor party B, they're going to owe an amogillion dollars.
        Assert.That(_sharedContractSys.TryBreachContract(contract, Party.PartyB));
        Assert.That(contract.Comp.State is ContractStateBreached { BreachingParty: Party.PartyB });
    }
}
