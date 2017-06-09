using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SpyStudio.Tools;

namespace SpyStudio.Swv.Registry
{
    public class SwvLayerRegistry : SwvLayerLikeThing
    {
        #region Properties

        public string Guid { get; protected set; }

        public SwvLayerRegistryBaseKey LocalMachine { get; protected set; }
        public SwvLayerRegistryBaseKey LocalMachine64 { get; protected set; }

        public SwvLayerRegistryBaseKey CurrentUser { get; protected set; }
        public SwvLayerRegistryBaseKey CurrentUser64 { get; protected set; }

        #endregion

        #region Instantiation

        public static SwvLayerRegistry Of(string aGuid)
        {
            return new SwvLayerRegistry(aGuid);
        }

        protected SwvLayerRegistry(string aGuid)
        {
            Guid = aGuid;

            var layerInfo = new Declarations.FSL2_INFO { fslGUID = aGuid, dwStructSize = (uint)Marshal.SizeOf(typeof(Declarations.FSL2_INFO)) };

            if (Declarations.FSL2GetLayerInfo(Guid, ref layerInfo) != 0)
                Debug.Assert(false, "Error getting layer info.");

            LocalMachine = SwvLayerRegistryBaseKey.LocalMachineFrom(layerInfo);
            LocalMachine64 = SwvLayerRegistryBaseKey.LocalMachine64From(layerInfo);
            CurrentUser = SwvLayerRegistryBaseKey.CurrentUserFrom(layerInfo);
            CurrentUser64 = SwvLayerRegistryBaseKey.CurrentUser64From(layerInfo);
        }

        #endregion

        #region Control

        public LayerEditor Edit()
        {
            return new LayerEditor(this);
        }

        #endregion
    }

    public interface SwvLayerLikeThing
    {
        string Guid { get; }
    }

    public class LayerEditor : IDisposable
    {
        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        private extern static uint VzStartLayerEdit(string fslGUID, uint flags);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        private extern static uint VzStopLayerEdit(string fslGUID, uint flags);

        private readonly string _guid;

        public class CouldNotSwitchLayerEditStateException : Exception
        {
            public bool PreviousState;
            public uint ErrorCode;
            public CouldNotSwitchLayerEditStateException(bool state, uint errorCode)
            {
                PreviousState = state;
                ErrorCode = errorCode;
            }
        }

        public LayerEditor(SwvLayerLikeThing layer): this(layer.Guid) {}

        public LayerEditor(string guid)
        {
            _guid = guid;
            uint error;
            if ((error = VzStartLayerEdit(_guid, Declarations.VZ_MOUNT_EDIT)) != 0)
                throw new CouldNotSwitchLayerEditStateException(false, error);
        }

        public void Dispose()
        {
            uint error;
            if ((error = VzStopLayerEdit(_guid, Declarations.VZ_DISMOUNT_EDIT)) != 0)
                throw new CouldNotSwitchLayerEditStateException(true, error);
        }
    }
}