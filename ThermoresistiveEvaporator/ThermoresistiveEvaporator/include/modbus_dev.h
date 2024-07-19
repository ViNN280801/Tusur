#pragma once
#include <iostream>
#include "modbus.h"
class modbus_dev
{
	private:
		modbus_t* ctx{ nullptr }; //A modbus device
	public:
		int connect();
		virtual int write_register(int addr, uint16_t val);
		virtual int write_bit(int addr, int val);
		virtual int read_holding_register(int addr);
		virtual int read_input_register(int addr);
		virtual ~modbus_dev() {
			modbus_free(ctx);
		}

};

