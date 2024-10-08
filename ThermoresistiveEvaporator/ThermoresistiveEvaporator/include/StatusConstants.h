#pragma once

#define STATUS_OK 0

// PS stands for "Power Supply".
#define PS_ERROR_INIT_CONNECTION_FAILED 1
#define PS_ERROR_SET_SLAVE_FAILED 2
#define PS_ERROR_CONNECT_FAILED 3
#define PS_ERROR_SET_CURRENT_FAILED 4
#define PS_ERROR_SET_VOLTAGE_FAILED 5
#define PS_ERROR_POWER_SUPPLY_TURN_ON_FAILED 6
#define PS_ERROR_POWER_SUPPLY_TURN_ON_WORKMODE_FAILED 7
#define PS_ERROR_RESET_CURRENT 8
#define PS_ERROR_RESET_VOLTAGE 9
#define PS_ERROR_RESET_WORKMODE 10
#define PS_ERROR_POWER_SUPPLY_TURN_OFF_FAILED 11
#define PS_ERROR_READ_CURRENT -1
#define PS_ERROR_READ_VOLTAGE -2
#define PS_ERROR_RESET_ZP_FAILED 12
#define PS_ERROR_UNSUPPORTED_TIMER_VALUE 13

// SM stands for "Step motor"
#define SM_ERROR_RW_HOLDING_REGISTER -1
#define SM_ERROR_INIT_CONNECTION_FAILED 1
#define SM_ERROR_SET_SLAVE_FAILED 2
#define SM_ERROR_CONNECT_FAILED 3
#define SM_ERROR_SET_512_REG_TO_1 4
#define SM_ERROR_SET_513_REG_TO_1 5
#define SM_ERROR_SET_512_REG_TO_0 6
#define SM_ERROR_SET_513_REG_TO_0 7
#define SM_ERROR_SHUTTER_ALREADY_CLOSED 8
