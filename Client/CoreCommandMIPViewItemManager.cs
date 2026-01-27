using System;
using System.Collections.Generic;
using System.Xml;
using CoreCommandMIP;
using VideoOS.Platform;
using VideoOS.Platform.Client;

namespace CoreCommandMIP.Client
{
    /// <summary>
    /// The ViewItemManager contains the configuration for the ViewItem. <br/>
    /// When the class is initiated it will automatically recreate relevant ViewItem configuration saved in the properties collection from earlier.
    /// Also, when the viewlayout is saved the ViewItemManager will supply current configuration to the SmartClient to be saved on the server.<br/>
    /// This class is only relevant when executing in the Smart Client.
    /// </summary>
    public class CoreCommandMIPViewItemManager : ViewItemManager
    {
        private Guid _someid;
        private string _someName;
        private List<Item> _configItems;
        private RemoteServerSettings _remoteSettings = new RemoteServerSettings();

        public CoreCommandMIPViewItemManager() : base("CoreCommandMIPViewItemManager")
        {
        }

        public event EventHandler ContextUpdated;

        #region Methods overridden 
        /// <summary>
        /// The properties for this ViewItem is now loaded into the base class and can be accessed via 
        /// GetProperty(key) and SetProperty(key,value) methods
        /// </summary>
		public override void PropertiesLoaded()
		{
			var savedId = GetProperty("SelectedGUID");
			_configItems = Configuration.Instance.GetItemConfigurations(CoreCommandMIPDefinition.CoreCommandMIPPluginId, null, CoreCommandMIPDefinition.CoreCommandMIPKind);
			if (!string.IsNullOrWhiteSpace(savedId) && Guid.TryParse(savedId, out var parsed) && _configItems != null)
			{
				SomeId = parsed;  // Set as last selected
			}
			else
			{
				AutoSelectFirstItem();
			}
		}

        ///// <summary>
        ///// Generate the UserControl containing the actual ViewItem Content.
        ///// 
        ///// For new plugins it is recommended to use GenerateViewItemWpfUserControl() instead. Only implement this one if support for Smart Clients older than 2017 R3 is needed.
        ///// </summary>
        ///// <returns></returns>
        //public override ViewItemUserControl GenerateViewItemUserControl()
        //{
        //	return new CoreCommandMIPViewItemUserControl(this);
        //}

        /// <summary>
        /// Generate the UserControl containing the actual ViewItem Content.
        /// </summary>
        /// <returns></returns>
        public override ViewItemWpfUserControl GenerateViewItemWpfUserControl()
        {
            return new CoreCommandMIPViewItemWpfUserControl(this);
        }

        ///// <summary>
        ///// Generate the UserControl containing the property configuration.
        ///// 
        ///// For new plugins it is recommended to use GeneratePropertiesWpfUserControl() instead. Only implement this one if support for Smart Clients older than 2017 R3 is needed.
        ///// </summary>
        ///// <returns></returns>
        //public override PropertiesUserControl GeneratePropertiesUserControl()
        //{
        //	return new CoreCommandMIPPropertiesUserControl(this);
        //}

        /// <summary>
        /// Generate the UserControl containing the property configuration.
        /// </summary>
        /// <returns></returns>
        public override PropertiesWpfUserControl GeneratePropertiesWpfUserControl()
        {
            return new CoreCommandMIPPropertiesWpfUserControl(this);
        }

        #endregion

        public List<Item> ConfigItems
        {
            get { return _configItems; }
        }

        public Guid SomeId
        {
            get { return _someid; }
            set
            {
                _someid = value;
                SetProperty("SelectedGUID", _someid.ToString());
                UpdateSelection(_configItems != null ? _configItems.Find(item => item.FQID.ObjectId == _someid) : null);
                SaveProperties();
                OnContextUpdated();
            }
        }

        public String SomeName
        {
            get { return _someName; }
            private set { _someName = value; }
        }

        internal RemoteServerSettings RemoteSettings
        {
            get { return _remoteSettings; }
        }

        private void UpdateSelection(Item selectedItem)
        {
            if (selectedItem == null)
            {
                SomeName = string.Empty;
                _remoteSettings = new RemoteServerSettings();
            }
            else
            {
                SomeName = selectedItem.Name;
                _remoteSettings = RemoteServerSettings.FromItem(selectedItem);
            }
        }

        private void AutoSelectFirstItem()
        {
            if (_configItems != null && _configItems.Count > 0)
            {
                SomeId = _configItems[0].FQID.ObjectId;
                return;
            }

            UpdateSelection(null);
            OnContextUpdated();
        }

        protected virtual void OnContextUpdated()
        {
            ContextUpdated?.Invoke(this, EventArgs.Empty);
        }
    }
}
