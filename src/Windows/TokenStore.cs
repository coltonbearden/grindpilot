using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace GrindPilot.Windows;

/// <summary>Where the update token is held. Split out so the update path can be tested with a token from elsewhere.</summary>
public interface ITokenStore
{
    string? Read();
    void Save(string token);
    void Delete();
}

/// <summary>
/// The door to Windows Credential Manager, holding an optional read-only GitHub token under the generic credential
/// <c>grindpilot-releases-token</c> (08-app-shape-and-stack.md, 8.6). Public releases need none; a token is for a build
/// that updates from a private fork. The value never reaches a log, a command line or the screen: only
/// <see cref="Read"/> returns it, to the update source.
/// </summary>
public sealed class CredentialManagerTokenStore(string target = CredentialManagerTokenStore.DefaultTarget) : ITokenStore
{
    public const string DefaultTarget = "grindpilot-releases-token";

    public string TargetName { get; } = target;

    private const int CredTypeGeneric = 1;
    private const int CredPersistLocalMachine = 2;
    private const int ErrorNotFound = 1168;

    public string? Read()
    {
        if (!CredReadW(TargetName, CredTypeGeneric, 0, out var ptr))
        {
            var error = Marshal.GetLastWin32Error();
            if (error == ErrorNotFound)
            {
                return null;
            }
            throw new Win32Exception(error, "Credential Manager couldn't read the update token.");
        }
        try
        {
            var cred = Marshal.PtrToStructure<Credential>(ptr);
            if (cred.CredentialBlob == IntPtr.Zero || cred.CredentialBlobSize == 0)
            {
                return null;
            }
            var bytes = new byte[cred.CredentialBlobSize];
            Marshal.Copy(cred.CredentialBlob, bytes, 0, bytes.Length);
            return Encoding.Unicode.GetString(bytes);
        }
        finally
        {
            CredFree(ptr);
        }
    }

    public void Save(string token)
    {
        token = token.Trim();
        if (token.Length == 0)
        {
            throw new ArgumentException("The token is empty.", nameof(token));
        }
        var bytes = Encoding.Unicode.GetBytes(token);
        var blob = Marshal.AllocHGlobal(bytes.Length);
        try
        {
            Marshal.Copy(bytes, 0, blob, bytes.Length);
            var cred = new Credential
            {
                Type = CredTypeGeneric,
                TargetName = TargetName,
                UserName = "GrindPilot",
                Comment = "Optional read-only GitHub token for GrindPilot's updates from a private repo",
                CredentialBlobSize = bytes.Length,
                CredentialBlob = blob,
                Persist = CredPersistLocalMachine,
            };
            if (!CredWriteW(ref cred, 0))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Credential Manager couldn't save the update token.");
            }
        }
        finally
        {
            Marshal.Copy(new byte[bytes.Length], 0, blob, bytes.Length);
            Marshal.FreeHGlobal(blob);
        }
    }

    public void Delete()
    {
        if (!CredDeleteW(TargetName, CredTypeGeneric, 0))
        {
            var error = Marshal.GetLastWin32Error();
            if (error != ErrorNotFound)
            {
                throw new Win32Exception(error, "Credential Manager couldn't delete the update token.");
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct Credential
    {
        public int Flags;
        public int Type;
        public string TargetName;
        public string? Comment;
        public long LastWritten;
        public int CredentialBlobSize;
        public IntPtr CredentialBlob;
        public int Persist;
        public int AttributeCount;
        public IntPtr Attributes;
        public string? TargetAlias;
        public string? UserName;
    }

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredReadW(string target, int type, int flags, out IntPtr credential);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredWriteW(ref Credential credential, int flags);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredDeleteW(string target, int type, int flags);

    [DllImport("advapi32.dll")]
    private static extern void CredFree(IntPtr buffer);
}

/// <summary>A token from an environment variable, for CI's release checks. Never from a command line.</summary>
public sealed class EnvironmentTokenStore(string variable) : ITokenStore
{
    public string? Read() => Environment.GetEnvironmentVariable(variable) is { Length: > 0 } t ? t : null;
    public void Save(string token) => throw new NotSupportedException("An environment token is read-only.");
    public void Delete() => throw new NotSupportedException("An environment token is read-only.");
}
