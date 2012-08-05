// TextureLib.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "TextureLib.hpp"
#include <stdint.h>
#include <math.h>

#define USE_SSE2

#ifdef USE_SSE2
#include <xmmintrin.h>
#endif

static uint8 shiftAmount[] = {24, 16, 8, 0};

Texture **gTextures;

Vector2 operator+(const Vector2 &a, const Vector2 &b) {
  return Vector2(a.x+b.x, a.y+b.y);
}

Vector2 operator-(const Vector2 &a, const Vector2 &b) {
  return Vector2(a.x-b.x, a.y-b.y);
}

Vector2 operator*(float s, const Vector2 &a) {
  return Vector2(s*a.x, s*a.y);
}

float len(const Vector2 &v) {
  return sqrtf(v.x*v.x+v.y*v.y);
}

Vector2 normalize(const Vector2 &v) {
  float d = len(v);
  return 1/d * v;
}

float dot(const Vector2 &a, const Vector2 &b) {
  return a.x*b.x + a.y*b.y;
}


// Perlin noise variables
int *gPerm;
Vector2 *gGrad;

void source_solid(int dstTexture, uint32 color) {
  Texture *texture = gTextures[dstTexture];
  float *p = (float *)texture->data;
  float rgba[4];
  for (int i = 0; i < 4; ++i)
    rgba[i] = ((color >> shiftAmount[i]) & 0xff)/ 255.0f;

#ifdef USE_SSE2
  __m128 col = _mm_load_ps(rgba);
  int len = texture->height * texture->width;
  for (int i = 0; i < len; ++i) {
    _mm_store_ps(p, col);
    p += 4;
  }
#else
  for (int i = 0; i < texture->height; ++i) {
    for (int j = 0; j  < texture->width; ++j) {
      p[0] = rgba[0];
      p[1] = rgba[1];
      p[2] = rgba[2];
      p[3] = rgba[3];
      p += 4;
    }
  }
#endif
}

float interpolate(float t) {
  return t*t*t*(t*(t*6-15)+10);
}

float perlin_noise(float x, float y) {
  // grid coordinates
  int gridX = ((int)x) & 0xff;
  int gridY = ((int)y) & 0xff;

  // get corner gradients
  Vector2 &g00 = gGrad[gPerm[gridX+gPerm[gridY]] % cNumGradients];
  Vector2 &g01 = gGrad[gPerm[gridX+1+gPerm[gridY]] % cNumGradients];
  Vector2 &g10 = gGrad[gPerm[gridX+gPerm[gridY+1]] % cNumGradients];
  Vector2 &g11 = gGrad[gPerm[gridX+1+gPerm[gridY+1]] % cNumGradients];

  // relative pos within cell
  Vector2 pos(x - gridX, y - gridY);

  // dot between gradient and vectors from grid cells to P
  float n00 = dot(g00, pos);
  float n01 = dot(g01, Vector2(pos.x-1, pos.y));
  float n10 = dot(g10, Vector2(pos.x, pos.y-1));
  float n11 = dot(g11, Vector2(pos.x-1, pos.y-1));

  // bilinear interpolate
  float fx = interpolate(pos.x);
  float nx0 = lerp(n00, n01, fx);
  float nx1 = lerp(n10, n11, fx);

  float fy = interpolate(pos.y);
  float nxy = lerp(nx0, nx1, fy);
  return nxy;
}

void source_noise(int dstTexture, float scaleX, float scaleY, float offsetX, float offsetY) {
  Texture *texture = gTextures[dstTexture];
  float *p = (float *)texture->data;

  int height = texture->height;
  int width = texture->width;
  for (int i = 0; i < height; ++i) {
    for (int j = 0; j  < width; ++j) {
      float x = offsetX + scaleX * j / width;
      float y = offsetY + scaleY * i / height;

      float n = perlin_noise(x, y);
      float n2 = perlin_noise(x+n, y);
      float n3 = perlin_noise(x+n2, y+2*n2);
      p[0] = n3;
      p[1] = n3;
      p[2] = n3;
      p[3] = n3;

      p += 4;
    }
  }
}

void source_turbulence(int dstTexture, int octaves, float scaleX, float scaleY, float offsetX, float offsetY) {
  Texture *texture = gTextures[dstTexture];
  float *p = (float *)texture->data;

  int height = texture->height;
  int width = texture->width;
  for (int i = 0; i < height; ++i) {
    for (int j = 0; j  < width; ++j) {
      float x = offsetX + scaleX * j / width;
      float y = offsetY + scaleY * i / height;

      float n = perlin_noise(x, y);
      float n2 = perlin_noise(x+n, y);
      float n3 = perlin_noise(x+n2, y+2*n2);
      p[0] = n3;
      p[1] = n3;
      p[2] = n3;
      p[3] = n3;

      p += 4;
    }
  }
}

void source_circles(int dstTexture, int amount, float size, float variance, uint32 innerColor, uint32 outerColor) {

  struct Circle {
    float x, y, radius;
  };
  const int cMaxCircles = 256;
  static Circle circles[cMaxCircles];
  amount = min(cMaxCircles, amount);

  for (int i = 0; i < amount; ++i) {
    circles[i].x = randf(0.0f, 1.0f);
    circles[i].y = randf(0.0f, 1.0f);
    circles[i].radius = size * size;  // saving size^2 to avoid a sqrt in the distance calc
  }
  
  Texture *texture = gTextures[dstTexture];
  float *p = (float *)texture->data;

  int height = texture->height;
  int width = texture->width;

  float inner[4], outer[4];

  for (int i = 0; i < 4; ++i) {
    inner[i] = ((innerColor >> shiftAmount[i]) & 0xff)/ 255.0f;
    outer[i] = ((outerColor >> shiftAmount[i]) & 0xff)/ 255.0f;
  }

  __m128 innerCol = _mm_loadu_ps(inner);
  __m128 outerCol = _mm_loadu_ps(outer);

  for (int i = 0; i < height; ++i) {
    for (int j = 0; j  < width; ++j) {

      float x = j / (float)width;
      float y = i / (float)height;

      _mm_store_ps(p, outerCol);
      for (int k = 0; k < cMaxCircles; ++k) {
        float dx = circles[k].x - x;
        float dy = circles[k].y - y;
        if (dx*dx+dy*dy < circles[k].radius) {
          _mm_store_ps(p, innerCol);
          break;
        }
      }
      p += 4;
    }
  }
}

enum BlendFunc {
  kBlendAdd,
  kBlendSub,
  kBlendMul,
  kBlendMin,
  kBlendMax
};

#ifdef USE_SSE2
void blend_inner(int dstTextureIdx, int srcTexture1Idx, float blend1, int srcTexture2Idx, float blend2, BlendFunc fn) {

  Texture *dstTexture = gTextures[dstTextureIdx];
  float *dst = dstTexture->data;

  Texture *srcTexture1 = gTextures[srcTexture1Idx];
  float *src1 = srcTexture1->data;

  Texture *srcTexture2 = gTextures[srcTexture2Idx];
  float *src2 = srcTexture2->data;

  ASSERT(dstTexture->width == srcTexture1->width && dstTexture->width == srcTexture2->width);
  ASSERT(dstTexture->height == srcTexture1->height && dstTexture->height == srcTexture2->height);

  int len = dstTexture->width * dstTexture->height;


  __m128 blend_a = _mm_set_ps1(blend1);
  __m128 blend_b = _mm_set_ps1(blend2);

  for (int i = 0; i < len; ++i) {
    __m128 tmp_a = _mm_mul_ps(_mm_load_ps(src1), blend_a);
    __m128 tmp_b = _mm_mul_ps(_mm_load_ps(src2), blend_b);
    switch (fn) {
      case kBlendAdd: tmp_a = _mm_add_ps(tmp_a, tmp_b); break;
      case kBlendSub: tmp_a = _mm_sub_ps(tmp_a, tmp_b); break;
      case kBlendMul: tmp_a = _mm_mul_ps(tmp_a, tmp_b); break;
      case kBlendMin: tmp_a = _mm_min_ps(tmp_a, tmp_b); break;
      case kBlendMax: tmp_a = _mm_max_ps(tmp_a, tmp_b); break;
      default: __assume(false);
    }
    _mm_store_ps(dst, tmp_a);
    dst += 4;
    src1 += 4;
    src2 += 4;
  }
}
#else
void blend_inner(float *dst, float *src1, float blend1, float *src2, float blend2, int len, BlendFunc fn) {

  for (int i = 0; i < len*4; ++i) {

    switch (fn) {
    case kBlendAdd: *dst = blend1 * *src1 + blend2 * *src2; break;
    case kBlendSub: *dst = blend1 * *src1 - blend2 * *src2; break;
    case kBlendMul: *dst = blend1 * *src1 * blend2 * *src2; break;
    case kBlendMin: *dst = min(blend1 * *src1, blend2 * *src2); break;
    case kBlendMax: *dst = max(blend1 * *src1, blend2 * *src2); break;
    default: __assume(false);
    }
    ++dst;
    ++src1;
    ++src2;
  }
}
#endif

void modifier_add(int dstTextureIdx, int srcTexture1Idx, float blend1, int srcTexture2Idx, float blend2) {
  blend_inner(dstTextureIdx, srcTexture1Idx, blend1, srcTexture2Idx, blend2, kBlendAdd);
}

void modifier_sub(int dstTextureIdx, int srcTexture1Idx, float blend1, int srcTexture2Idx, float blend2) {
  blend_inner(dstTextureIdx, srcTexture1Idx, blend1, srcTexture2Idx, blend2, kBlendSub);
}

void modifier_max(int dstTextureIdx, int srcTexture1Idx, float blend1, int srcTexture2Idx, float blend2) {
  blend_inner(dstTextureIdx, srcTexture1Idx, blend1, srcTexture2Idx, blend2, kBlendMax);
}

void modifier_min(int dstTextureIdx, int srcTexture1Idx, float blend1, int srcTexture2Idx, float blend2) {
  blend_inner(dstTextureIdx, srcTexture1Idx, blend1, srcTexture2Idx, blend2, kBlendMin);
}

void modifier_mul(int dstTextureIdx, int srcTexture1Idx, float blend1, int srcTexture2Idx, float blend2) {
  blend_inner(dstTextureIdx, srcTexture1Idx, blend1, srcTexture2Idx, blend2, kBlendMul);
}

