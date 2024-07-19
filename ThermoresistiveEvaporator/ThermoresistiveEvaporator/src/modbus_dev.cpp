#include "modbus_dev.h"
#include "modbus.h"
#include <iostream>

using namespace std;

int modbus_dev::connect() {
	//To connect a modbus device
	//Assuming that the port to connect to is COM3
	ctx = modbus_new_rtu("COM3", 115200, 'N', 8, 1);
	if (ctx == nullptr) {
		cout << "Failed to create instance" << endl;
		return -1;
	}
	modbus_set_slave(ctx, 3);//Set slave-id to 3
	if (modbus_connect(ctx) == -1) {
		cout << "Connection failed" << endl;
		modbus_free(ctx);
		return -1;
	}
	cout << "Connection status: established!" << endl;
}
int modbus_dev::write_register(int addr, uint16_t val) {
	//Function to write data
	//Assuming that the data to be written has been obtained and set to uint16_t val
	if (modbus_write_register(ctx, addr, val) == -1) {
		cout << "Writing failed" << endl;
		return -1;
	}
	cout << "Wrote " << val << " to register " << addr << endl;
	return 0;
}
int modbus_dev::write_bit(int addr, int val) {
	//To write bits to a modbus device
	//The value 'int val' can be either 0 or 1
	if (modbus_write_bit(ctx, addr, val) == -1) {
		cout << "Failed to write bit" << endl;
		return -1;
	}
	cout << "Coil " << addr << " set to " << val << endl;
	return 0;
}
int modbus_dev::read_holding_register(int addr) {
	//To read a modbus register
	uint16_t readbacks[100]{};
	if (modbus_read_registers(ctx, addr, 1, readbacks) == -1) {
		cout << "Reading failed" << endl;
		return -1;
	}
	cout << "The data from register " << addr << " is " << readbacks[0] << endl;
	return static_cast<int>(readbacks[0]);
}
int modbus_dev::read_input_register(int addr) {
	//To read an input register
	uint16_t readbacks[100]{};
	if (modbus_read_input_registers(ctx, addr, 1, readbacks) == -1) {
		cout << "Reading failed" << endl;
		return -1;
	}
	cout << "The data from register " << addr << " is " << readbacks[0] << endl;
	return static_cast<int>(readbacks[0]);
}

