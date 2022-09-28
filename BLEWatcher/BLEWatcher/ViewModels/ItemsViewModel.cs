using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;

using Xamarin.Forms;

using BLEWatcher.Models;
using BLEWatcher.Views;
using Plugin.BLE.Abstractions.Contracts;

namespace BLEWatcher.ViewModels
{
    public class ItemsViewModel : BaseViewModel
    {
        public ObservableCollection<Item> Items { get; set; }
        public Command LoadItemsCommand { get; set; }

        public ItemsViewModel()
        {
            Title = "Browse";
            Items = new ObservableCollection<Item>();
            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());

            MessagingCenter.Subscribe<NewItemPage, Item>(this, "AddItem", HandleNewItemAsync);


            Task.Run(ObserveBleDevicesAsync);

        }

        private async void HandleNewItemAsync(object sender, object item)
        {
            var newItem = (Item)item;
            Items.Add(newItem);
            await DataStore.AddItemAsync(newItem);
        }

        async Task ObserveBleDevicesAsync()
        {
            var adapter = Plugin.BLE.CrossBluetoothLE.Current.Adapter;
            adapter.ScanTimeout = 300000;

            adapter.DeviceDiscovered += Adapter_DeviceDiscovered;
            try
            {
                Items.Add(new Item { Text = "????" });
                await adapter.StartScanningForDevicesAsync();
                Items.Add(new Item { Text = "....." });
                while (true)
                {
                    await Task.Delay(2000);

                    Items.Clear();

                    Items.Add(new Item { Text = "<---" });
                    foreach (var device in adapter.DiscoveredDevices)
                    {
                        Items.Add(new Item { Text = device.Id.ToString() });

                    }
                    Items.Add(new Item { Text = "--->" });
                }

            }
            catch (Exception e)
            {
                Items.Add(new Item { Text = $"Exception: {e}" });

            }

            await adapter.StopScanningForDevicesAsync();

        }

        private void Adapter_DeviceDiscovered(object sender, Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs e)
        {
            Items.Add(new Item { Text = "Adding:" });

            var device = e.Device;
            Items.Add(new Item { Text = device.Id.ToString() });
        }

        async Task ExecuteLoadItemsCommand()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                //await Task.CompletedTask;
                await ReloadStoredItemsAsyc();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ReloadStoredItemsAsyc()
        {
            Items.Clear();
            var items = await DataStore.GetItemsAsync(true);
            foreach (var item in items)
            {
                Items.Add(item);
            }
        }
    }
}