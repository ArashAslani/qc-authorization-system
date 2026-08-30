namespace AccessManagement.Tests.TestSupport;

/// <summary>
/// Well-known GUID constants for tests (stable across runs). All values are valid hex GUIDs.
/// </summary>
public static class TestGuids
{
    public static readonly Guid CompanyA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid CompanyB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public static readonly Guid PosA1 = Guid.Parse("a1111111-1111-1111-1111-111111111111");
    public static readonly Guid PosA2 = Guid.Parse("a2222222-2222-2222-2222-222222222222");
    public static readonly Guid PosB1 = Guid.Parse("b1111111-1111-1111-1111-111111111111");

    public static readonly Guid Personnel1 = Guid.Parse("c1111111-1111-1111-1111-111111111111");
    public static readonly Guid Permission1 = Guid.Parse("d1111111-1111-1111-1111-111111111111");
    public static readonly Guid Permission100 = Guid.Parse("d0000100-0000-0000-0000-000000000100");
    public static readonly Guid Role1 = Guid.Parse("e1111111-1111-1111-1111-111111111111");
    public static readonly Guid RoleGroup1 = Guid.Parse("f1111111-1111-1111-1111-111111111111");

    public static readonly Guid Grant1 = Guid.Parse("01111111-1111-1111-1111-111111111111");
    public static readonly Guid Delegation1 = Guid.Parse("02222222-2222-2222-2222-222222222222");

    public static readonly Guid Assignment101 = Guid.Parse("01000101-0000-0000-0000-000000000101");
    public static readonly Guid Assignment102 = Guid.Parse("01000102-0000-0000-0000-000000000102");
    public static readonly Guid Assignment103 = Guid.Parse("01000103-0000-0000-0000-000000000103");

    public static readonly Guid Position200 = Guid.Parse("02000200-0000-0000-0000-000000000200");
    public static readonly Guid Subject50 = Guid.Parse("00000050-0000-0000-0000-000000000050");

    public static readonly Guid Laboratory1 = Guid.Parse("11111111-1111-1111-1111-111111111101");
    public static readonly Guid Laboratory2 = Guid.Parse("11111111-1111-1111-1111-111111111102");

    public static readonly Guid ControlPlan101 = Guid.Parse("c0000101-0000-0000-0000-000000000101");
    public static readonly Guid ControlPlan201 = Guid.Parse("c0000201-0000-0000-0000-000000000201");
    public static readonly Guid ControlPlan301 = Guid.Parse("c0000301-0000-0000-0000-000000000301");
    public static readonly Guid ControlPlan302 = Guid.Parse("c0000302-0000-0000-0000-000000000302");
    public static readonly Guid ControlPlan401 = Guid.Parse("c0000401-0000-0000-0000-000000000401");
    public static readonly Guid Bom501 = Guid.Parse("b0000501-0000-0000-0000-000000000501");
}
