#include <iostream>
#include <vector>
#include <array>
#include <cstring>

std::vector<double> line_to_coords(std::vector<char> bytes)
{
    std::vector<double> ret;

    for (int i = 0; i < bytes.size(); i += 8)
    {
        double d;
        char b[] = {bytes[i], bytes[i + 1], bytes[i + 2], bytes[i + 3], bytes[i + 4], bytes[i + 5], bytes[i + 6], bytes[i + 7]};
        memcpy(&d, &b, sizeof(d));
        ret.push_back(d);
    }

    return ret;
}

template <unsigned long size>
std::vector<char> coords_to_line(std::array<double, size> coords)
{
    std::vector<char> ret;

    for (int i = 0; i < coords.size(); i++)
    {
        char *bytes = reinterpret_cast<char *>(&coords[i]);
        for (int j = 0; j < 8; j++)
        {
            ret.push_back(bytes[j]);
        }
    }

    return ret;
}


int main()
{
    // Test line_to_coords function

    std::array<double, 3> coords = {3.14, 2.718, 1.0};
    std::cout << "Coordinates: ";
    for (const auto &coord : coords)
    {
        std::cout << coord << " ";
    }
    std::vector<char> bytes = coords_to_line(coords);
    std::cout << "\nBytes: ";
    for (const auto &byte : bytes)
    {
        std::cout << (int)byte << " ";
    }
    std::vector<double> coords2 = line_to_coords(bytes);
    std::cout << "\nCoordinates: ";

    for (const auto &coord : coords2)
    {
        std::cout << coord << " ";
    }
    std::cout << "\n";
    
}
