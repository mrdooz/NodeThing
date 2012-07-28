// TextureLib.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"

extern "C"
{
  __declspec(dllexport) void FillHwnd(HWND hwnd, int width, int height) {
    HDC dc = GetDC(hwnd);

    BITMAPINFO info;
    ZeroMemory(&info, sizeof(info));
    info.bmiHeader.biBitCount = 32;
    info.bmiHeader.biWidth = width;
    info.bmiHeader.biWidth = -height;

    void *bits;
    CreateDIBSection(dc, &info, DIB_RGB_COLORS, &bits, NULL, 0);

    ReleaseDC(hwnd, dc);

  }
};

