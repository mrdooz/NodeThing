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

#ifndef EXCLUDE_UNUSED
#define USE_SRC_SOLID 1
#define USE_SRC_NOISE 1
#define USE_SRC_CIRCLES 1
#define USE_SRC_RANDOM 1
#define USE_SRC_SINEWAVES 1
#define USE_SRC_PLASMA 1
#define USE_MOD_ADD 1
#define USE_MOD_SUB 1
#define USE_MOD_MAX 1
#define USE_MOD_MIN 1
#define USE_MOD_MUL 1
#define USE_MOD_SCALE 1
#define USE_MOD_DISTORT 1
#define USE_MOD_BLUR 1
#endif

struct Texture {
  float *data;    // RGBA
  float *scratch; // enough space to hold 3*width pixels, to avoid having to do clamping operations
  int width;
  int height;
};

extern Texture **gTextures;

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
extern int gPerm[512];
extern Vector2 gGrad[cNumGradients];


enum TextureMode {
  kTextureClamp,
  kTextureWrap,
  kTextureMirror,
};

enum BlurDirection {
  kBlurHoriz,
  kBlurVert,
  kBlurBoth,
};

#if USE_SRC_SOLID
void __cdecl source_solid(int dstTexture, uint32 color_argb);
#endif
#if USE_SRC_NOISE
void __cdecl source_noise(int dstTexture, float scaleX, float scaleY, float offsetX, float offsetY);
#endif
#if USE_SRC_CIRCLES
void __cdecl source_circles(int dstTexture, int amount, float size, float variance, float fade, uint32 innerColor_argb, uint32 outerColor_argb, uint32 seed);
#endif
#if USE_SRC_RANDOM
void __cdecl source_random(int dstTexture, float scale, uint32 seed);
#endif
#if USE_SRC_SINEWAVES
void __cdecl source_sinwaves(int dstTexture, float scale, int numSin, int func, float startAmp, float endAmp, float startPhase, float endPhase, float startFreq, float endFreq);
#endif
#if USE_SRC_PLASMA
void __cdecl source_plasma(int dstTexture, float scale, int monochrome, int depth, int seed);
#endif

#if USE_MOD_SCALE
void __cdecl modifier_scale(int dstTextureIdx, int srcTextureIdx, float blend);
#endif
#if USE_MOD_ADD
void __cdecl modifier_add(int dstTextureIdx, int srcTexture1Idx, int srcTexture2Idx);
#endif
#if USE_MOD_SUB
void __cdecl modifier_sub(int dstTextureIdx, int srcTexture1Idx, int srcTexture2Idx);
#endif
#if USE_MOD_MIN
void __cdecl modifier_min(int dstTextureIdx, int srcTexture1Idx, int srcTexture2Idx);
#endif
#if USE_MOD_MAX
void __cdecl modifier_max(int dstTextureIdx, int srcTexture1Idx, int srcTexture2Idx);
#endif
#if USE_MOD_MUL
void __cdecl modifier_mul(int dstTextureIdx, int srcTexture1Idx, int srcTexture2Idx);
#endif
#if USE_MOD_INVERT
void __cdecl modifier_invert(int dstTextureIdx, int srcTextureIdx);
#endif
#if USE_MOD_GRAYSCALE
void __cdecl modifier_grayscale(int dstTextureIdx, int srcTextureIdx);
#endif
#if USE_MOD_DISTORT
void __cdecl modifier_map_distort(int dstTextureIdx, int srcTextureIdx, int distortTextureIdx, float scale, int channels);
#endif
#if USE_MOD_BLUR
void __cdecl modifier_blur(int dstTextureIdx, int srcTextureIdx, float blurRadius, TextureMode mode, BlurDirection dir);
#endif

