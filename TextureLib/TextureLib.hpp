#pragma once

#include <stdint.h>

typedef int16_t int16;

typedef uint32_t uint32;
typedef uint16_t uint16;
typedef uint8_t uint8;

#ifdef _DEBUG
#define ASSERT(x) do { if (!(x)) _asm {int 3} } while(false);
#else
#define ASSERT(x) __assume(x);
#endif

struct Texture {
  float *data;  // RGBA
  int width;
  int height;
};

extern Texture **gTextures;
/*
struct Vector3 {
  Vector3() {}
  Vector3(float x, float y, float z) : x(x), y(y), z(z) {}
  float x, y, z;
};
*/
struct Vector2 {
  Vector2() {}
  Vector2(float x, float y) : x(x), y(y) {}
  float x, y;
};

extern Vector2 operator+(const Vector2 &a, const Vector2 &b);
extern Vector2 operator-(const Vector2 &a, const Vector2 &b);
extern Vector2 operator*(float s, const Vector2 &a);
extern float len(const Vector2 &v);
extern Vector2 normalize(const Vector2 &v);
extern float dot(const Vector2 &a, const Vector2 &b);

extern int gRandomSeed;
extern int tRand();
extern float tGaussianRand(float mean, float variance);

#ifndef RAND_MAX
#define RAND_MAX 0x7fff
#endif

template <typename T>
T randf(T a, T b) {
  return a + (b-a) * tRand() / RAND_MAX;
}

template <typename T, typename U>
T lerp(T a, T b, U v) {
  return a + (v * (b-a));
}

static const int cNumGradients = 16;

void source_solid(int dstTexture, uint32 color_argb);
void source_noise(int dstTexture, float scaleX, float scaleY, float offsetX, float offsetY);
void source_circles(int dstTexture, int amount, float size, float variance, float fade, uint32 innerColor_argb, uint32 outerColor_argb, uint32 seed);
void source_random(int dstTexture, float scale, uint32 seed);
void source_sinwaves(int dstTexture, float scale, int numSin, int func, float startAmp, float endAmp, float startPhase, float endPhase, float startFreq, float endFreq);
void source_plasma(int dstTexture, float scale, int monochrome, int startOctave, int endOctave, int seed);

void modifier_add(int dstTextureIdx, int srcTexture1Idx, float blend1, int srcTexture2Idx, float blend2);
void modifier_sub(int dstTextureIdx, int srcTexture1Idx, float blend1, int srcTexture2Idx, float blend2);
void modifier_min(int dstTextureIdx, int srcTexture1Idx, float blend1, int srcTexture2Idx, float blend2);
void modifier_max(int dstTextureIdx, int srcTexture1Idx, float blend1, int srcTexture2Idx, float blend2);
void modifier_mul(int dstTextureIdx, int srcTexture1Idx, float blend1, int srcTexture2Idx, float blend2);
void modifier_invert(int dstTextureIdx, int srcTextureIdx);
void modifier_grayscale(int dstTextureIdx, int srcTextureIdx);
void modifier_map_distort(int dstTextureIdx, int srcTextureIdx, int distortTextureIdx, float scale, int channels);

