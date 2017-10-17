#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2017 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#region Using Statements
using System;
using System.IO;
using System.Threading;
#endregion

namespace Microsoft.Xna.Framework.Storage
{
	/// <summary>
	/// Exposes a storage device for storing user data.
	/// </summary>
	/// <remarks>
	/// MSDN documentation contains related conceptual article:
	/// http://msdn.microsoft.com/en-us/library/bb200105.aspx
	/// </remarks>
	public sealed class StorageDevice
	{
		#region Public Properties

		/// <summary>
		/// Returns the amount of free space.
		/// </summary>
		public long FreeSpace
		{
			get
			{
				try
				{
					return drive.AvailableFreeSpace;
				}
				catch(Exception e)
				{
					// Storage root was invalid or unavailable.
					throw new StorageDeviceNotConnectedException(
						"The storage device bound to the container is not connected.",
						e
					);
				}
			}
		}

		/// <summary>
		/// Returns true if this StorageDevice path is accessible, false otherwise.
		/// </summary>
		public bool IsConnected
		{
			get
			{
				try
				{
					return drive.IsReady;
				}
				catch
				{
					// The storageRoot path is invalid / has been removed.
					return false;
				}
			}
		}

		/// <summary>
		/// Returns the total size of device.
		/// </summary>
		public long TotalSpace
		{
			get
			{
				try
				{
					return drive.TotalSize;
				}
				catch(Exception e)
				{
					// Storage root was invalid or unavailable.
					throw new StorageDeviceNotConnectedException(
						"The storage device bound to the container is not connected.",
						e
					);
				}
			}
		}

		#endregion

		#region Private Variables

		private PlayerIndex? devicePlayer;

		#endregion

		#region Private Static Variables

		private static readonly string storageRoot = FNAPlatform.GetStorageRoot();
		private static readonly DriveInfo drive = new DriveInfo(MonoPathRootWorkaround());

		#endregion

		#region Events

		/// <summary>
		/// Fired when a device is removed or inserted.
		/// </summary>
		public static event EventHandler<EventArgs> DeviceChanged;

		private void OnDeviceChanged()
		{
			if (DeviceChanged != null)
			{
				DeviceChanged(this, null);
			}
		}

		#endregion

		#region Private XNA Lies

		private class NotAsyncLie : IAsyncResult
		{
			public object AsyncState
			{
				get;
				private set;
			}

			public bool CompletedSynchronously
			{
				get
				{
					return true;
				}
			}

			public bool IsCompleted
			{
				get
				{
					return true;
				}
			}

			public WaitHandle AsyncWaitHandle
			{
				get;
				private set;
			}

			public NotAsyncLie(object state)
			{
				AsyncState = state;
				AsyncWaitHandle = new ManualResetEvent(true);
			}
		}

		private class ShowSelectorLie : NotAsyncLie
		{
			public readonly PlayerIndex? PlayerIndex;

			public ShowSelectorLie(object state, PlayerIndex? playerIndex) : base(state)
			{
				PlayerIndex = playerIndex;
			}
		}

		private class OpenContainerLie : NotAsyncLie
		{
			public readonly string DisplayName;

			public OpenContainerLie(object state, string displayName) : base(state)
			{
				DisplayName = displayName;
			}
		}

		#endregion

		#region Internal Constructors

		internal StorageDevice(PlayerIndex? player)
		{
			devicePlayer = player;
		}

		#endregion

		#region Public OpenContainer Methods

		/// <summary>
		/// Begins the open for a StorageContainer.
		/// </summary>
		/// <returns>The open StorageContainer.</returns>
		/// <param name="displayName">Name of file.</param>
		/// <param name="callback">Method to call on completion.</param>
		/// <param name="state">Request identifier object for callback (can be null).</param>
		public IAsyncResult BeginOpenContainer(
			string displayName,
			AsyncCallback callback,
			object state
		) {
			IAsyncResult result = new OpenContainerLie(state, displayName);
			if (callback != null)
			{
				callback(result);
			}
			return result;
		}

		/// <summary>
		/// Ends the open container process.
		/// </summary>
		/// <returns>The open StorageContainer.</returns>
		/// <param name="result">Result of BeginOpenContainer.</param>
		public StorageContainer EndOpenContainer(IAsyncResult result)
		{
			return new StorageContainer(
				this,
				(result as OpenContainerLie).DisplayName,
				storageRoot,
				devicePlayer
			);
		}

		#endregion

		#region Public ShowSelector Methods

		/// <summary>
		/// Begin process to display the StorageDevice selector UI.
		/// </summary>
		/// <returns>The show selector.</returns>
		/// <param name="callback">Method to invoke when device is selected by player.</param>
		/// <param name="state">Request identifier object for callback (can be null).</param>
		public static IAsyncResult BeginShowSelector(
			AsyncCallback callback,
			object state
		) {
			return BeginShowSelector(
				0,
				0,
				callback,
				state
			);
		}

		/// <summary>
		/// Begin process to display the StorageDevice selector UI.
		/// </summary>
		/// <returns>The show selector.</returns>
		/// <param name="player">The PlayerIndex. Only PlayerIndex.One is valid on Windows.</param>
		/// <param name="callback">Method to invoke when device is selected by player.</param>
		/// <param name="state">Request identifier object for callback (can be null).</param>
		public static IAsyncResult BeginShowSelector(
			PlayerIndex player,
			AsyncCallback callback,
			object state
		) {
			return BeginShowSelector(
				player,
				0,
				0,
				callback,
				state
			);
		}

		/// <summary>
		/// Begin process to display the StorageDevice selector UI.
		/// </summary>
		/// <returns>The show selector.</returns>
		/// <param name="sizeInBytes">Size (in bytes) of data to write.</param>
		/// <param name="directoryCount">Number of directories to write.</param>
		/// <param name="callback">Method to invoke when device is selected by player.</param>
		/// <param name="state">Request identifier object for callback (can be null).</param>
		public static IAsyncResult BeginShowSelector(
			int sizeInBytes,
			int directoryCount,
			AsyncCallback callback,
			object state
		) {
			IAsyncResult result = new ShowSelectorLie(state, null);
			if (callback != null)
			{
				callback(result);
			}
			return result;
		}

		/// <summary>
		/// Begin process to display the StorageDevice selector UI.
		/// </summary>
		/// <returns>The show selector.</returns>
		/// <param name="player">The PlayerIndex. Only PlayerIndex.One is valid on Windows.</param>
		/// <param name="sizeInBytes">Size (in bytes) of data to write.</param>
		/// <param name="directoryCount">Number of directories to write.</param>
		/// <param name="callback">Method to invoke when device is selected by player.</param>
		/// <param name="state">Request identifier object for callback (can be null).</param>
		public static IAsyncResult BeginShowSelector(
			PlayerIndex player,
			int sizeInBytes,
			int directoryCount,
			AsyncCallback callback,
			object state
		) {
			IAsyncResult result = new ShowSelectorLie(state, player);
			if (callback != null)
			{
				callback(result);
			}
			return result;
		}

		/// <summary>
		/// Ends the show selector user interface display.
		/// </summary>
		/// <returns>The storage device.</returns>
		/// <param name="result">The result of BeginShowSelector.</param>
		public static StorageDevice EndShowSelector(IAsyncResult result)
		{
			return new StorageDevice((result as ShowSelectorLie).PlayerIndex);
		}

		#endregion

		#region Public StorageContainer Delete Method

		public void DeleteContainer(string titleName)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Private Static Methods

		private static string MonoPathRootWorkaround()
		{
			if (Environment.OSVersion.Platform == PlatformID.Win32NT)
			{
				// This is what we should be doing everywhere...
				return Path.GetPathRoot(storageRoot);
			}

			// This is stolen from Mono's Path.cs
			if (storageRoot == null)
			{
				return null;
			}
			if (storageRoot.Trim().Length == 0)
			{
				throw new ArgumentException("The specified path is not of a legal form.");
			}
			if (!Path.IsPathRooted(storageRoot))
			{
				return string.Empty;
			}

			/* FIXME: Mono bug!
			 *
			 * For Unix, the Mono Path.GetPathRoot is pretty lazy:
			 * https://github.com/mono/mono/blob/master/mcs/class/corlib/System.IO/Path.cs#L443
			 * It should actually be checking the drives and
			 * comparing them to the provided path.
			 * If a Mono maintainer is reading this, please steal
			 * this code so we don't have to hack around Mono!
			 *
			 * -flibit
			 */
			int drive = -1, length = 0;
			string[] drives = Environment.GetLogicalDrives();
			for (int i = 0; i < drives.Length; i += 1)
			{
				if (	storageRoot.StartsWith(drives[i]) &&
					drives[i].Length > length	)
				{
					drive = i;
					length = drives[i].Length;
				}
			}
			if (drive >= 0)
			{
				return drives[drive];
			}

			// Uhhhhh
			return Path.GetPathRoot(storageRoot);
		}

		#endregion
	}
}
