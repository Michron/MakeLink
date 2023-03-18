namespace MakeLink.NativeTypes
{
    /// <summary>
    /// Each reparse point has a reparse tag. The reparse tag uniquely identifies the owner of that reparse point. The
    /// owner is the implementer of the file system filter driver associated with a reparse tag.
    /// For more info:
    /// <see href="https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-fscc/c8e77b37-3909-4fe6-a4ea-2b9d423b1ee4">2.1.2.1 Reparse Tags</see>
    /// </summary>
    /// <remarks>
    /// Reparse tags are exposed to clients for third-party applications. Those applications can set, get, and process
    /// reparse tags as needed. Third parties MUST request a reserved reparse tag value to ensure that conflicting tag
    /// values do not occur.
    /// </remarks>
    internal enum ReparseTag : uint
    {
        /// <summary>
        /// Used for mount point support, specified in section
        /// <see href="https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-fscc/ca069dad-ed16-42aa-b057-b6b207f447cc">2.1.2.5</see>.
        /// </summary>
        MountPoint = 0xA0000003,

        /// <summary>
        /// Used for <see href="https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-fscc/8ac44452-328c-4d7b-a784-d72afd19bd9f#gt_04f1ed93-15cb-4090-8204-c43bec8c7398">symbolic link</see> support.
        /// See section <see href="https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-fscc/b41f1cbf-10df-4a47-98d4-1c52a833d913">2.1.2.4</see>.
        /// </summary>
        Symlink = 0xA000000C
    }
}
