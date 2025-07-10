# Minimum required version of CMake
cmake_minimum_required(VERSION 3.16)

# Project name and version
project(Slicer VERSION 1.0 LANGUAGES CXX)

# Set the C++ standard
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED True)

# Define the source files
file(GLOB_RECURSE SRC_FILES src/*.cpp)
file(GLOB_RECURSE HEADER_FILES src/*.hpp)

# Include directories for headers
include_directories(src)

# Add the assimp library
set(ASSIMP_DIR lib/assimp-5.4.3)
add_subdirectory(${ASSIMP_DIR})

# Add the executable target
add_executable(${PROJECT_NAME} ${SRC_FILES} ${HEADER_FILES})

# Link the assimp library to your project
target_link_libraries(${PROJECT_NAME} PRIVATE assimp)

# Set include directories for the assimp library
target_include_directories(${PROJECT_NAME} PRIVATE ${ASSIMP_DIR}/include)

# Use g++ on macOS
if(APPLE)
    set(CMAKE_CXX_COMPILER g++)
    set(CMAKE_C_COMPILER gcc)

    # Add rpath for locating dynamic libraries on macOS
    set(CMAKE_MACOSX_RPATH 1)
    set(CMAKE_INSTALL_RPATH "@loader_path/../lib")
endif()
