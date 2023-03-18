using System.Runtime.InteropServices;

namespace MakeLink.NativeTypes
{
    /// <summary>
    /// Managed version of the native <see href="https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntifs/ns-ntifs-_reparse_data_buffer">REPARSE_DATA_BUFFER</see> type.
    /// The REPARSE_DATA_BUFFER structure contains reparse point data for a Microsoft reparse point.
    /// </summary>
    /// <remarks>
    /// The REPARSE_DATA_BUFFER structure is used by Microsoft file systems, filters, and minifilter drivers, as well
    /// as the I/O manager, to store data for a reparse point.
    /// This structure can only be used for Microsoft reparse points.Third-party reparse point owners must use the
    /// <see href="https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntifs/ns-ntifs-_reparse_guid_data_buffer">REPARSE_GUID_DATA_BUFFER</see>
    /// structure instead.
    /// Microsoft reparse points can use the REPARSE_DATA_BUFFER structure or the REPARSE_GUID_DATA_BUFFER structure.
    /// From the union, you can use the GenericReparseBuffer structure to interpret the payload for any
    /// IO_REPARSE_TAG_XXX tag, or optionally use one of the other structures within the union as follows:
    /// - Use the SymbolicLinkReparseBuffer structure when FileTag is IO_REPARSE_TAG_SYMLINK.
    /// - Use the MountPointReparseBuffer structure when FileTag is IO_REPARSE_TAG_MOUNT_POINT.
    /// For more information about reparse point tags, see the Microsoft Windows SDK documentation.
    /// </remarks>
    internal readonly struct ReparseDataBuffer
    {
        private readonly uint _reparseTag;
        private readonly ushort _reparseDataLength;
        private readonly ushort _reserved;
        private readonly ushort _substituteNameOffset;
        private readonly ushort _substituteNameLength;
        private readonly ushort _printNameOffset;
        private readonly ushort _printNameLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x3FF0)]
        private readonly byte[] _pathBuffer;

        /// <summary>
        /// Reparse point tag. Must be a Microsoft reparse point tag.
        /// </summary>
        public ReparseTag ReparseTag
        {
            get => (ReparseTag)_reparseTag;
            init => _reparseTag = (uint)value;
        }

        /// <summary>
        /// Size, in bytes, of the reparse data in the buffer that DataBuffer points to.. This can be calculated by:
        /// (4 * sizeof(ushort)) + SubstituteNameLength + PrintNameLength + 
        /// (namesAreNullTerminated ? 2 * sizeof(char) : 0);
        /// </summary>
        public ushort ReparseDataLength
        {
            get => _reparseDataLength;
            init => _reparseDataLength = value;
        }

        /// <summary>
        /// Length, in bytes, of the unparsed portion of the file name pointed to by the FileName member of the
        /// associated file object. For more information about the FileName member, see
        /// <see href="https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/wdm/ns-wdm-_file_object">FILE_OBJECT</see>.
        /// This member is only valid for create operations when the I/O fails with STATUS_REPARSE. For all other
        /// purposes, such as setting or querying a reparse point for the reparse data, this member is treated as
        /// reserved.
        /// </summary>
        public ushort Reserved
        {
            get => _reserved;
            init => _reserved = value;
        }

        /// <summary>
        /// Offset, in bytes, of the substitute name string in the <see cref="PathBuffer" /> array.
        /// Note that this offset must be divided by sizeof(WCHAR) to get the array index.
        /// </summary>
        public ushort SubstituteNameOffset
        {
            get => _substituteNameOffset;
            init => _substituteNameOffset = value;
        }

        /// <summary>
        /// Length, in bytes, of the substitute name string. If this string is null-terminated,
        /// SubstituteNameLength does not include space for the null character.
        /// </summary>
        public ushort SubstituteNameLength
        {
            get => _substituteNameLength;
            init => _substituteNameLength = value;
        }

        /// <summary>
        /// Offset, in bytes, of the print name string in the <see cref="PathBuffer" /> array.
        /// Note that this offset must be divided by sizeof(WCHAR) to get the array index.
        /// </summary>
        public ushort PrintNameOffset
        {
            get => _printNameOffset;
            init => _printNameOffset = value;
        }

        /// <summary>
        /// Length, in bytes, of the print name string. If this string is null-terminated,
        /// PrintNameLength does not include space for the null character. 
        /// </summary>
        public ushort PrintNameLength
        {
            get => _printNameLength;
            init => _printNameLength = value;
        }

        /// <summary>
        /// A buffer containing the unicode-encoded path string. The path string contains
        /// the substitute name string and print name string.
        /// </summary>
        public byte[] PathBuffer
        {
            get => _pathBuffer;
            init => _pathBuffer = value;
        }
    }
}
