#ifndef UTILITIES_HPP
#define UTILITIES_HPP

#include <chrono>
#include <concepts>
#include <filesystem>
#include <format>
#include <iostream>
#include <source_location>

#define EXTRACT_FILE_NAME(filepath) std::filesystem::path(std::string(filepath).c_str()).filename().string()
#define ERRMSG(desc) std::cerr << std::format("\033[1;31mError:\033[0m\033[1m {}: {}({} line): {}: {}\n",     \
                                              getCurTime(),                                                   \
                                              EXTRACT_FILE_NAME(std::source_location::current().file_name()), \
                                              std::source_location::current().line(),                         \
                                              __PRETTY_FUNCTION__, desc);
#define LOGMSG(desc) std::clog << std::format("Log: {}: {}({} line): {}: {}\n",                               \
                                              getCurTime(),                                                   \
                                              EXTRACT_FILE_NAME(std::source_location::current().file_name()), \
                                              std::source_location::current().line(),                         \
                                              __PRETTY_FUNCTION__, desc);
#define SIMPLE_ERR_MSG(desc) std::cerr << std::format("\033[1;31mError:\033[0m\033[1m {}: {}\n", \
                                                      getCurTime(), desc);
#define SIMPLE_LOG_MSG(desc) std::clog << std::format("Log: {}: {}\n", \
                                                      getCurTime(), desc);

/**
 * @brief Gets the current system time in the specified format.
 * @tparam Format A format string compatible with std::put_time.
 * Defaults to "%H:%M:%S" if not specified.
 * For example, "%Y-%m-%d %H:%M:%S" for date and time in YYYY-MM-DD HH:MM:SS format.
 * @param format The format string compatible with std::put_time. Defaults to "%H:%M:%S".
 */
std::string getCurTime(std::string_view format = "%H:%M:%S")
{
    std::chrono::system_clock::time_point tp{std::chrono::system_clock::now()};
    time_t tt{std::chrono::system_clock::to_time_t(tp)};
    tm *t{localtime(&tt)};
    std::stringstream ss;
    ss << std::put_time(t, std::string(format).c_str());
    return ss.str();
}

/**
 * @brief Concept that checks if variable has output operator
 * @tparam a variable to check
 * @param os output stream
 */
template <typename T>
concept Printable = requires(T a, std::ostream &os) {
    {
        os << a
    } -> std::same_as<std::ostream &>;
};

/**
 * @brief Generates string with specified multiple args
 * @tparam args arguments of type that can be convert to string
 * @return String composed from all arguments
 */
template <Printable... Args>
std::string stringify(Args &&...args)
{
    std::ostringstream oss;
    (oss << ... << std::forward<Args>(args));
    return oss.str();
}

#endif // !UTILITIES_HPP
