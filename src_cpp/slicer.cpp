#include <iostream>
#include <cstdlib>
#include <string>
#include <vector>
#include <assimp/Importer.hpp>      // C++ importer interface
#include <assimp/scene.h>           // Output data structure
#include <assimp/postprocess.h>     // Post processing flags

using namespace std;


int main()
{
    // Get object file path from enviroment file
    auto object_file = getenv("OBJECT_FILES_PATH");

    if (object_file == nullptr) {
        cout << "Object files path not found. Please add it to you .env file." << endl;
        // return 1;
    } else {
        cout << "Object files path: " << object_file << endl;
    }
    // Hardcoded input file for testing
    // string input_file = "test.stl";

    auto obj_file_path = std::string("path/to/your/object/file");

    // Read object file using assimp importer

    Assimp::Importer importer;
    const aiScene *scene = importer.ReadFile(
        obj_file_path,
        aiProcess_Triangulate | aiProcess_JoinIdenticalVertices
    );

    // Print basic information about the object file
    cout << "Object file: " << obj_file_path << endl;
    cout << "Number of meshes: " << scene->mNumMeshes << endl;
    cout << "Number of materials: " << scene->mNumMaterials << endl;


    

}