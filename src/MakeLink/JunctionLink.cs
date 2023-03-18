using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using MakeLink.NativeTypes;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Storage.FileSystem;

namespace MakeLink
{
    public class JunctionLink
    {
        private enum ReparseWriteCommand : uint
        {
            Create = 0x000900A4,
            Delete = 0x000900AC
        }

        private enum ReparseReadCommand : uint
        {
            GetTargetPoint = 0x000900A8
        }

        /// <summary>
        /// This prefix indicates to NTFS that the path is to be treated as a non-interpreted
        /// path in the virtual file system.
        /// </summary>
        private const string NonInterpretedPathPrefix = @"\??\";

        /// <summary>
        /// Creates a directory junction, linking a directory at <paramref name="link"/> to the directory at <paramref name="target"/>.
        /// </summary>
        /// <param name="link">The directory path that links to <paramref name="target"/>.</param>
        /// <param name="target">The target directory of the junction link.</param>
        /// <param name="overwrite">If <see langword="true"/>, an existing directory or junction will be overwritten.</param>
        /// <exception cref="DirectoryNotFoundException">Thrown if the <paramref name="target"/> directory does not exist.</exception>
        /// <exception cref="IOException">
        /// Thrown if a directory already exists at <paramref name="link"/> and <paramref name="overwrite"/> is set to <see langword="false"/>,
        /// or if the junction link could not be created.
        /// </exception>
        [SupportedOSPlatform("windows5.1.2600")]
        public static void Create(string link, string target, bool overwrite = false)
        {
            target = Path.GetFullPath(target);

            EnsureExistingTarget(target);
            EnsureValidLink(link, overwrite);

            using SafeFileHandle handle = OpenReparsePoint(link, FILE_ACCESS_FLAGS.FILE_GENERIC_WRITE);

            // TODO Replace "\\.\" on the target with ""?
            ReadOnlySpan<byte> bytes = Encoding.Unicode.GetBytes($"{NonInterpretedPathPrefix}{target}");

            ReparseDataBuffer dataBuffer = new()
            {
                ReparseTag = ReparseTag.MountPoint,
                ReparseDataLength = (ushort)(bytes.Length + 12),
                SubstituteNameOffset = 0,
                SubstituteNameLength = (ushort)bytes.Length,
                PrintNameOffset = (ushort)(bytes.Length + 2),
                PrintNameLength = 0,
                PathBuffer = new byte[0x3ff0]
            };

            bytes.CopyTo(dataBuffer.PathBuffer);

            WriteReparseData(handle, dataBuffer, bufferSize: (uint)bytes.Length + 20, command: ReparseWriteCommand.Create);
        }

        /// <summary>
        /// Deletes the directory junction at the path specified by <paramref name="link"/>.
        /// </summary>
        /// <param name="link">The path to a directory junction to delete.</param>
        /// <exception cref="IOException">
        /// Thrown if <paramref name="link"/> does not refer to a junction link or directory,
        /// or if the junction link could not be deleted.
        /// </exception>
        [SupportedOSPlatform("windows5.1.2600")]
        public static void Delete(string link)
        {
            if (!Directory.Exists(link))
            {
                if (File.Exists(link))
                {
                    throw new IOException($"The path at {link} is not a junction link or directory.");
                }

                return;
            }

            using SafeFileHandle handle = OpenReparsePoint(link, FILE_ACCESS_FLAGS.FILE_GENERIC_WRITE);

            ReparseDataBuffer dataBuffer = new()
            {
                ReparseTag = ReparseTag.MountPoint,
                ReparseDataLength = 0,
                PathBuffer = new byte[0x3ff0]
            };

            WriteReparseData(handle, dataBuffer, bufferSize: 8, command: ReparseWriteCommand.Delete);

            try
            {
                Directory.Delete(link);
            }
            catch (IOException ex)
            {
                throw new IOException("Unable to delete junction link.", ex);
            }
        }

        /// <summary>
        /// Determines whether the specified directory exists and refers to a junction link.
        /// </summary>
        /// <param name="link">The path to verify.</param>
        /// <returns>
        /// <see langword="true"/> if a directory at <paramref name="link"/> exists, and is a junction link, <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="IOException">Thrown if the specified path is invalid or reparse data could not be read.</exception>
        [SupportedOSPlatform("windows5.1.2600")]
        public static bool Exists(string link)
        {
            if (!Directory.Exists(link))
            {
                return false;
            }

            using SafeFileHandle handle = OpenReparsePoint(link, FILE_ACCESS_FLAGS.FILE_GENERIC_READ);

            if (!ReadReparseData(handle, ReparseReadCommand.GetTargetPoint, out ReparseDataBuffer dataBuffer))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the target directory of the specified <paramref name="link"/>.
        /// </summary>
        /// <param name="link">The path to the junction link of which to get the target directory.</param>
        /// <returns>
        /// The path to the target directory of the junction link, or <see langword="null"/> if the link is not a junction point or doesn't exist.
        /// </returns>
        /// <exception cref="IOException">Thrown if the specified path is invalid or reparse data could not be read.</exception>
        [SupportedOSPlatform("windows5.1.2600")]
        public static string? GetTarget(string link)
        {
            if (!Directory.Exists(link))
            {
                return null;
            }

            using SafeFileHandle handle = OpenReparsePoint(link, FILE_ACCESS_FLAGS.FILE_GENERIC_READ);

            if (!ReadReparseData(handle, ReparseReadCommand.GetTargetPoint, out ReparseDataBuffer dataBuffer))
            {
                return null;
            }

            string target = Encoding.Unicode.GetString(dataBuffer.PathBuffer, dataBuffer.SubstituteNameOffset, dataBuffer.SubstituteNameLength);

            if (target.StartsWith(NonInterpretedPathPrefix))
            {
                target = target[NonInterpretedPathPrefix.Length..];
            }

            return target;
        }

        [SupportedOSPlatform("windows5.1.2600")]
        private static void WriteReparseData(SafeFileHandle handle, in ReparseDataBuffer dataBuffer, uint bufferSize, ReparseWriteCommand command)
        {
            int inBufferSize = Marshal.SizeOf<ReparseDataBuffer>();
            IntPtr inBuffer = Marshal.AllocHGlobal(inBufferSize);

            try
            {
                Marshal.StructureToPtr(dataBuffer, inBuffer, false);
                uint bytesReturned;

                unsafe
                {
                    bool result = PInvoke.DeviceIoControl(
                        handle,
                        (uint)command,
                        inBuffer.ToPointer(),
                        bufferSize,
                        null,
                        0,
                        &bytesReturned,
                        null);

                    ThrowIfCommandFailed(result, command);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(inBuffer);
            }
        }

        [SupportedOSPlatform("windows5.1.2600")]
        private static bool ReadReparseData(SafeFileHandle handle, ReparseReadCommand command, out ReparseDataBuffer dataBuffer)
        {
            const int ErrorNotAReparsePoint = 4390;

            int outBufferSize = Marshal.SizeOf<ReparseDataBuffer>();
            IntPtr outBuffer = Marshal.AllocHGlobal(outBufferSize);

            try
            {
                uint bytesReturned;

                unsafe
                {
                    bool result = PInvoke.DeviceIoControl(
                        handle,
                        (uint)command,
                        null,
                        0,
                        outBuffer.ToPointer(),
                        (uint)outBufferSize,
                        &bytesReturned,
                        null);

                    if (!result && Marshal.GetLastWin32Error() == ErrorNotAReparsePoint)
                    {
                        dataBuffer = default;

                        return false;
                    }

                    ThrowIfCommandFailed(result, command);
                }

                dataBuffer = Marshal.PtrToStructure<ReparseDataBuffer>(outBuffer);
            }
            finally
            {
                Marshal.FreeHGlobal(outBuffer);
            }

            if (dataBuffer.ReparseTag != ReparseTag.MountPoint)
            {
                return false;
            }

            return true;
        }

        [SupportedOSPlatform("windows5.1.2600")]
        private static SafeFileHandle OpenReparsePoint(string reparsePoint, FILE_ACCESS_FLAGS accessMode)
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(5, 1, 2600))
            {
                throw new NotSupportedException("Non-windows platforms are currently not supported.");
            }

            const FILE_SHARE_MODE DefaultFileShareMode = FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE | FILE_SHARE_MODE.FILE_SHARE_DELETE;
            const FILE_CREATION_DISPOSITION DefaultFileCreationDisposition = FILE_CREATION_DISPOSITION.OPEN_EXISTING;
            const FILE_FLAGS_AND_ATTRIBUTES DefaultFileAttributes = FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_OPEN_REPARSE_POINT;

            SafeFileHandle reparsePointHandle = PInvoke.CreateFile(reparsePoint, accessMode, DefaultFileShareMode, null, DefaultFileCreationDisposition, DefaultFileAttributes, null);

            ThrowOnWin32Error("Unable to open reparse point.");

            return reparsePointHandle;
        }

        private static void EnsureExistingTarget(string target)
        {
            if (!Directory.Exists(target))
            {
                throw new DirectoryNotFoundException($"The directory at {target} does not exist, or is not a directory.");
            }
        }

        private static void EnsureValidLink(string link, bool overwrite)
        {
            if (Directory.Exists(link))
            {
                if (!overwrite)
                {
                    throw new IOException($"A directory at {link} already exists, and overwrite is set to false.");
                }
            }
            else
            {
                Directory.CreateDirectory(link);
            }
        }

        private static void ThrowOnWin32Error(string message)
        {
            if (Marshal.GetLastWin32Error() != 0)
            {
                throw new IOException(message, Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
            }
        }

        private static void ThrowIfCommandFailed([DoesNotReturnIf(false)] bool result, ReparseWriteCommand command)
        {
            if (!result)
            {
                string message = command switch
                {
                    ReparseWriteCommand.Create => "Unable to create junction link.",
                    ReparseWriteCommand.Delete => "Unable to delete junction link.",
                    _ => throw new NotImplementedException()
                };

                throw new IOException(message, Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
            }
        }

        private static void ThrowIfCommandFailed([DoesNotReturnIf(false)] bool result, ReparseReadCommand command)
        {
            if (!result)
            {
                string message = command switch
                {
                    ReparseReadCommand.GetTargetPoint => "Unable to get target path of junction link.",
                    _ => throw new NotImplementedException()
                };

                throw new IOException(message, Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
            }
        }
    }
}