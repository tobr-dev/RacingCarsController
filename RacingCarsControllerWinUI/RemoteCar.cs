﻿using System;
using System.Threading.Tasks;

namespace RacingCarsControllerWinUI
{
    public interface IRemoteCar : IAsyncDisposable
    {
        event EventHandler<int> BatteryLevelChanged;

        Task SendCommandAsync(CarCommand command);
        Task SubscribeToBatteryNotifications();
    }

    public abstract class RemoteCar : IRemoteCar
    {
        public abstract string ControlServiceUUID { get; }   
        public abstract string ControlCharacteristicUUID { get; }

        public abstract string BatteryServiceUUID { get; }
        public abstract string BatteryCharacteristicUUID { get; }

        public event EventHandler<int> BatteryLevelChanged;

        protected IBLEDevice Device;

        protected bool IgnoreSubsequentSameCommands;
        private CarCommand _previousCommand;

        public RemoteCar(IBLEDevice device)
        {
            Device = device;
            Device.CharacteristicChanged += Device_CharacteristicChanged;
            IgnoreSubsequentSameCommands = true;
        }

        private void Device_CharacteristicChanged(object sender, byte[] args)
        {
            var level = GetBatteryLevel(args);
            App.WriteDebug($"Battery level is {level}");
            OnBatteryLevelChanged(level);
        }

        public virtual async Task SendCommandAsync(CarCommand command)
        {
            if (IgnoreSubsequentSameCommands && command == _previousCommand)
            {
                App.WriteDebug($"Don't send command {command}");
                return;
            };
            _previousCommand = command;
            App.WriteDebug($"Send command {command}");

            var data = PreparePayload(command);
            await Device.WriteCharacteristics(ControlServiceUUID, ControlCharacteristicUUID, data);
        }

        public virtual async Task SubscribeToBatteryNotifications()
        {
            await Device.SubscribeToNotifications(BatteryServiceUUID, BatteryCharacteristicUUID);
        }

        protected abstract byte[] PreparePayload(CarCommand command);

        protected abstract int GetBatteryLevel(byte[] data);

        protected virtual void OnBatteryLevelChanged(int level)
        {
            BatteryLevelChanged?.Invoke(this, level);
        }

        public async ValueTask DisposeAsync()
        {
            Device.CharacteristicChanged -= Device_CharacteristicChanged;
            await Device.DisposeAsync();
        }
    }
}
