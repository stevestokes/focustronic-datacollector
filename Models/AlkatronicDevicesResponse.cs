using System;
using System.Collections.Generic;
using System.Text;

public class Settings
{
    public int next_test_time { get; set; }
    public int upper_kh { get; set; }
    public int lower_kh { get; set; }
    public int measure_interval { get; set; }
    public int aquarium_volume { get; set; }
    public int reagent_alert { get; set; }
    public int dose_buffer_alert { get; set; }
    public int kh_alert { get; set; }
    public int baseline_calibration { get; set; }
    public int is_action_mode_on { get; set; }
    public int is_washout_mode_on { get; set; }
    public int is_dosetronic_mode_on { get; set; }
    public int is_fast_mode_on { get; set; }
    public int correction_method { get; set; }
    public int retest_duration { get; set; }
    public string pump_d_action { get; set; }
    public int settings_update_time { get; set; }
    public int current_test_count { get; set; }
    public int test_count_limit { get; set; }
}

public class Versions
{
    public bool is_allow_cloud_settings { get; set; }
    public bool is_allow_dosetronic_settings { get; set; }
    public bool is_allow_k_bnc_settings { get; set; }
    public bool is_allow_mtest_phtest_refill_settings { get; set; }
    public bool is_allow_sda_oda_retest_settings { get; set; }
    public bool is_allow_plug_settings { get; set; }
    public bool is_use_record_format_v2 { get; set; }
    public bool is_allow_reset_ph_probe_calibration_settings { get; set; }
    public bool is_allow_super_low_reference_settings { get; set; }
}

public class Options
{
    public int aquarium_volume_upper_limit { get; set; }
    public int low_reference_range_from { get; set; }
    public int low_reference_range_to { get; set; }
    public int high_reference_range_from { get; set; }
    public int high_reference_range_to { get; set; }
    public int has_k_value_settings { get; set; }
    public int? is_require_passcode { get; set; }
}

public class EligibleAction
{
    public string key { get; set; }
    public Options options { get; set; }
}

public class Parameter
{
    public string parameter { get; set; }
    public int latest_record { get; set; }
    public int low_reference { get; set; }
    public int high_reference { get; set; }
    public int multiply_factor { get; set; }
    public int is_action_mode_on { get; set; }
    public int is_automatic_mode_on { get; set; }
    public int baseline_device_value { get; set; }
    public int baseline_reference_value { get; set; }
    public int record_time { get; set; }
}

public class Device
{
    public int id { get; set; }
    public int user_id { get; set; }
    public string serial_number { get; set; }
    public int aquarium_tank_id { get; set; }
    public string friendly_name { get; set; }
    public string firmware_version { get; set; }
    public string next_firmware_version { get; set; }
    public string mcu_version { get; set; }
    public int batch_no { get; set; }
    public int is_auto_update { get; set; }
    public int last_online { get; set; }
    public int is_active { get; set; }
    public bool is_beta_test { get; set; }
    public string local_ip_address { get; set; }
    public int last_calibrate_a { get; set; }
    public int last_calibrate_c { get; set; }
    public int last_calibrate_d { get; set; }
    public int last_calibrate_p { get; set; }
    public int last_reset_test_counter_time { get; set; }
    public Settings settings { get; set; }
    public Versions versions { get; set; }
    public List<EligibleAction> eligible_actions { get; set; }
    public List<Parameter> parameters { get; set; }
}

public class AlkatronicDevices
{
    public string type { get; set; }
    public List<Device> devices { get; set; }
}

public class AlkatronicDevicesResponse
{
    public bool result { get; set; }
    public string message { get; set; }
    public List<AlkatronicDevices> data { get; set; }
}


