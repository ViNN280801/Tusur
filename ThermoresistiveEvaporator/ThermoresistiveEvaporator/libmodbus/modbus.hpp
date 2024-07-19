#include <modbus.h>


#include "utilities.hpp"

/**
 * @brief Establishes a connection to a Modbus device using RTU.
 * @details This function initializes a libmodbus context for an RTU over serial line and attempts to connect to the device.
 * @param port The serial port to connect to (e.g., "/dev/ttyS0").
 * @param slave_id The slave ID on the Modbus network.
 * @return A pointer to the modbus_t context, or nullptr if the connection fails.
 */
modbus_t *establishConnection(char const *port, int slave_id)
{
    modbus_t *ctx = modbus_new_rtu(//./COM10, 115200, 'N', 8, 1);
    if (ctx == nullptr)
    {
        SIMPLE_ERR_MSG("Unable to create the libmodbus context");
        return nullptr;
    }

    modbus_set_slave(ctx, slave_id);

    if (modbus_connect(ctx) == -1)
    {
        modbus_free(ctx);

        SIMPLE_ERR_MSG(stringify("Connection failed: ", modbus_strerror(errno)));
        return nullptr;
    }

    SIMPLE_LOG_MSG("Connection status: established");
    return ctx;
}

/**
 * @brief Reads a value from a Modbus register.
 * @details This function reads a single register from the connected Modbus device.
 * @param ctx The modbus context.
 * @param reg The register address to read from.
 * @param dest The destination variable where the read value will be stored.
 * @return 0 on success, -1 on error.
 */
int readValueFromRegister(modbus_t *ctx, int reg, uint16_t *dest)
{
    int rc = modbus_read_registers(ctx, reg, 1, dest);
    if (rc == -1)
    {
        SIMPLE_ERR_MSG(stringify("Failed to read register: ", modbus_strerror(errno)));
        return -1;
    }
    SIMPLE_LOG_MSG(stringify("Value from register ", reg, ": ", dest[0]));
    return 0;
}

/**
 * @brief Writes a value to a Modbus register.
 * @details This function writes a single value to a register on the connected Modbus device.
 * @param ctx The modbus context.
 * @param reg The register address to write to.
 * @param value The value to write to the register.
 * @return 0 on success, -1 on error.
 */
int writeValueToRegister(modbus_t *ctx, int reg, uint16_t value)
{
    int rc = modbus_write_register(ctx, reg, value);
    if (rc == -1)
    {
        ERRMSG(stringify("Failed to write register: ", modbus_strerror(errno)));
        return -1;
    }
    SIMPLE_LOG_MSG(stringify("Value in register ", reg, " changed to ", value));
    return 0;
}
