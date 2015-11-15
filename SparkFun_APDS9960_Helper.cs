using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;
using Windows.UI.Xaml;

namespace MirroZ.RaspberryPi.Components.ComponentModel.Hardware
{
    /// <summary>
    /// Provide functions to use the SparkFun_APDS9960 driver
    /// </summary>
    public class SparkFun_APDS9960_Helper : IDisposable
    {
        #region Enumerations

        /// <summary>
        /// Define which mode the sensor should use
        /// </summary>
        public enum Mode
        {
            Gesture,
            Light,
            Proximity
        }

        #endregion

        #region Fields

        /// <summary>
        /// Represents the device
        /// </summary>
        private I2cDevice _i2CDevice;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the SparkFun_APDS9960
        /// </summary>
        private SparkFun_APDS9960 Sensor { get; set; }

        /// <summary>
        /// Gets or sets a timer used when the sensor is in mode Gesture
        /// </summary>
        private DispatcherTimer GestureTimer { get; set; }

        #endregion

        #region Event

        /// <summary>
        /// Raised when the sensor detect a new gesture
        /// </summary>
        public event EventHandler<SparkFun_APDS9960.DirectionDefinitions> GestureDetected;

        #endregion

        #region Constuctors

        /// <summary>
        /// Initialize a new instance of <see cref="SparkFun_APDS9960_Helper"/>
        /// </summary>
        /// <param name="mode">The mode of the sensor</param>
        public SparkFun_APDS9960_Helper(Mode mode)
        {
            Initialize(mode);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initialize everything
        /// </summary>
        /// <param name="mode">The mode of the sensor</param>
        private async void Initialize(Mode mode)
        {
            await InitializeI2C();
            InitializeSensor(mode);
            if (mode == Mode.Gesture)
            {
                InitializeGestureTimer();
            }
        }

        /// <summary>
        /// Initialize the device
        /// </summary>
        /// <returns></returns>
        private async Task InitializeI2C()
        {
            var settings = new I2cConnectionSettings(SparkFun_APDS9960.APDS9960_I2C_ADDR);
            settings.BusSpeed = I2cBusSpeed.FastMode;
            settings.SharingMode = I2cSharingMode.Shared;

            var devices = await DeviceInformation.FindAllAsync(I2cDevice.GetDeviceSelector("I2C1"));

            if (devices.Count == 0)
            {
                throw new Exception("SparkFun APDS-9960 device not found");
            }

            _i2CDevice = await I2cDevice.FromIdAsync(devices[0].Id, settings);
        }

        /// <summary>
        /// Initialize the sensor
        /// </summary>
        /// <param name="mode">The mode of the sensor</param>
        private void InitializeSensor(Mode mode)
        {
            Sensor = new SparkFun_APDS9960(ref _i2CDevice);
            Sensor.Initialize();

            switch (mode)
            {
                case Mode.Gesture:
                    Sensor.EnableGestureSensor(true);
                    break;
                case Mode.Light:
                    Sensor.EnableLightSensor(true);
                    break;
                case Mode.Proximity:
                    Sensor.EnableProximitySensor(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }

        /// <summary>
        /// Initialize the timer if the mode of the sensor is in Gesture
        /// </summary>
        private void InitializeGestureTimer()
        {
            GestureTimer = new DispatcherTimer();
            GestureTimer.Interval = TimeSpan.FromMilliseconds(100);
            GestureTimer.Tick += GestureTimer_Tick;
            GestureTimer.Start();
        }

        /// <summary>
        /// Gets a value that represents the proximity
        /// </summary>
        /// <returns>Returns a value between 0 and 255</returns>
        public byte GetProximity()
        {
            return Sensor.ReadProximity();
        }

        /// <summary>
        /// Gets a value that represents the ambiant light
        /// </summary>
        /// <returns>Returns an integer that can be used to calculate light level (Lux) or color temperature (Kelvin)</returns>
        public int GetAmbientLight()
        {
            return Sensor.ReadAmbientLight();
        }

        /// <summary>
        /// Gets a value that represents the green light
        /// </summary>
        /// <returns>Returns an integer that can be used to calculate light level (Lux) or color temperature (Kelvin)</returns>
        public int GetGreenLight()
        {
            return Sensor.ReadGreenLight();
        }

        /// <summary>
        /// Gets a value that represents the red light
        /// </summary>
        /// <returns>Returns an integer that can be used to calculate light level (Lux) or color temperature (Kelvin)</returns>
        public int GetRedLight()
        {
            return Sensor.ReadRedLight();
        }

        /// <summary>
        /// Gets a value that represents the blue light
        /// </summary>
        /// <returns>Returns an integer that can be used to calculate light level (Lux) or color temperature (Kelvin)</returns>
        public int GetBlueLight()
        {
            return Sensor.ReadBlueLight();
        }

        public void Dispose()
        {
            if (GestureTimer != null)
            {
                GestureTimer.Stop();
                GestureTimer.Tick -= GestureTimer_Tick;
                GestureTimer = null;
            }

            Sensor.DisableGestureSensor();
            Sensor.DisableLightSensor();
            Sensor.DisableProximitySensor();
            Sensor.DisablePower();
            Sensor = null;

            _i2CDevice.Dispose();
            _i2CDevice = null;
        }

        #endregion

        #region Handled Methods

        private void GestureTimer_Tick(object sender, object e)
        {
            if (GestureDetected != null && Sensor.IsGestureAvailable())
            {
                GestureDetected(this, Sensor.ReadGesture());
            }
        }

        #endregion
    }
}
