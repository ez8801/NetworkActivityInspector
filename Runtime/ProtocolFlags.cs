namespace EZ.Network
{
    [System.Flags]
    public enum Protocol
    {
        Http = 1 << 1,
        Tcp = 1 << 2,
        Everything = ~0
    }
}