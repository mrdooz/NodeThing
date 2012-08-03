// TextureLib.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "TextureLib.hpp"
#include <stdint.h>
#include <math.h>

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
  if (d > 0.0f) {
    return 1/d * v;
  }
  return v;
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
  float r = ((color >> 24) & 0xff)/ 255.0f;
  float g = ((color >> 16) & 0xff) / 255.0f;
  float b = ((color >>  8) & 0xff) / 255.0f;
  float a = ((color >>  0) & 0xff) / 255.0f;

  for (int i = 0; i < texture->height; ++i) {
    for (int j = 0; j  < texture->width; ++j) {
      p[0] = r;
      p[1] = g;
      p[2] = b;
      p[3] = a;
      p += 4;
    }
  }
}

float interpolate(float t) {
  return t*t*t*(t*(t*6-15)+10);
  float t2 = t*t;
  float t3 = t2*t;
  float t4 = t2*t2;
  float t5 = t3*t2;
  return 6*t5-15*t4+10*t3;
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

void source_noise(int dstTexture, float scaleX, float scaleY) {
  Texture *texture = gTextures[dstTexture];
  float *p = (float *)texture->data;

  int height = texture->height;
  int width = texture->width;


  for (int i = 0; i < height; ++i) {
    for (int j = 0; j  < width; ++j) {
      float x = scaleX * j / width;
      float y = scaleY * i / height;

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

void modifier_add(int dstTextureIdx, int srcTexture1Idx, float blend1, int srcTexture2Idx, float blend2) {

  Texture *dstTexture = gTextures[dstTextureIdx];
  float *dst = dstTexture->data;

  Texture *srcTexture1 = gTextures[srcTexture1Idx];
  float *src1 = srcTexture1->data;

  Texture *srcTexture2 = gTextures[srcTexture2Idx];
  float *src2 = srcTexture2->data;

  ASSERT(dstTexture->width == srcTexture1->width && dstTexture->width == srcTexture2->width);
  ASSERT(dstTexture->height == srcTexture1->height && dstTexture->height == srcTexture2->height);

  for (int i = 0; i < dstTexture->height; ++i) {
    for (int j = 0; j < dstTexture->width; ++j) {
      dst[0] = blend1 * src1[0] + blend2 * src2[0];
      dst[1] = blend1 * src1[1] + blend2 * src2[1];
      dst[2] = blend1 * src1[2] + blend2 * src2[2];
      dst[3] = blend1 * src1[3] + blend2 * src2[3];
      dst += 4;
      src1 += 4;
      src2 += 4;
    }
  }
}

void modifier_sub(int dstTextureIdx, int srcTexture1Idx, float blend1, int srcTexture2Idx, float blend2) {
  Texture *dstTexture = gTextures[dstTextureIdx];
  float *dst = dstTexture->data;

  Texture *srcTexture1 = gTextures[srcTexture1Idx];
  float *src1 = srcTexture1->data;

  Texture *srcTexture2 = gTextures[srcTexture2Idx];
  float *src2 = srcTexture2->data;

  ASSERT(dstTexture->width == srcTexture1->width && dstTexture->width == srcTexture2->width);
  ASSERT(dstTexture->height == srcTexture1->height && dstTexture->height == srcTexture2->height);

  for (int i = 0; i < dstTexture->height; ++i) {
    for (int j = 0; j < dstTexture->width; ++j) {
      dst[0] = blend1 * src1[0] - blend2 * src2[0];
      dst[1] = blend1 * src1[1] - blend2 * src2[1];
      dst[2] = blend1 * src1[2] - blend2 * src2[2];
      dst[3] = blend1 * src1[3] - blend2 * src2[3];
      dst += 4;
      src1 += 4;
      src2 += 4;
    }
  }

}

void modifier_max(int dstTextureIdx, int srcTexture1Idx, float blend1, int srcTexture2Idx, float blend2) {
  Texture *dstTexture = gTextures[dstTextureIdx];
  float *dst = dstTexture->data;

  Texture *srcTexture1 = gTextures[srcTexture1Idx];
  float *src1 = srcTexture1->data;

  Texture *srcTexture2 = gTextures[srcTexture2Idx];
  float *src2 = srcTexture2->data;

  ASSERT(dstTexture->width == srcTexture1->width && dstTexture->width == srcTexture2->width);
  ASSERT(dstTexture->height == srcTexture1->height && dstTexture->height == srcTexture2->height);

  for (int i = 0; i < dstTexture->height; ++i) {
    for (int j = 0; j < dstTexture->width; ++j) {
      dst[0] = max(blend1 * src1[0], blend2 * src2[0]);
      dst[1] = max(blend1 * src1[1], blend2 * src2[1]);
      dst[2] = max(blend1 * src1[2], blend2 * src2[2]);
      dst[3] = max(blend1 * src1[3], blend2 * src2[3]);
      dst += 4;
      src1 += 4;
      src2 += 4;
    }
  }
}

void modifier_min(int dstTextureIdx, int srcTexture1Idx, float blend1, int srcTexture2Idx, float blend2) {
  Texture *dstTexture = gTextures[dstTextureIdx];
  float *dst = dstTexture->data;

  Texture *srcTexture1 = gTextures[srcTexture1Idx];
  float *src1 = srcTexture1->data;

  Texture *srcTexture2 = gTextures[srcTexture2Idx];
  float *src2 = srcTexture2->data;

  ASSERT(dstTexture->width == srcTexture1->width && dstTexture->width == srcTexture2->width);
  ASSERT(dstTexture->height == srcTexture1->height && dstTexture->height == srcTexture2->height);

  for (int i = 0; i < dstTexture->height; ++i) {
    for (int j = 0; j < dstTexture->width; ++j) {
      dst[0] = min(blend1 * src1[0], blend2 * src2[0]);
      dst[1] = min(blend1 * src1[1], blend2 * src2[1]);
      dst[2] = min(blend1 * src1[2], blend2 * src2[2]);
      dst[3] = min(blend1 * src1[3], blend2 * src2[3]);
      dst += 4;
      src1 += 4;
      src2 += 4;
    }
  }

}

void modifier_mul(int dstTextureIdx, int srcTexture1Idx, float blend1, int srcTexture2Idx, float blend2) {

  Texture *dstTexture = gTextures[dstTextureIdx];
  float *dst = dstTexture->data;

  Texture *srcTexture1 = gTextures[srcTexture1Idx];
  float *src1 = srcTexture1->data;

  Texture *srcTexture2 = gTextures[srcTexture2Idx];
  float *src2 = srcTexture2->data;

  ASSERT(dstTexture->width == srcTexture1->width && dstTexture->width == srcTexture2->width);
  ASSERT(dstTexture->height == srcTexture1->height && dstTexture->height == srcTexture2->height);

  for (int i = 0; i < dstTexture->height; ++i) {
    for (int j = 0; j < dstTexture->width; ++j) {
      dst[0] = blend1 * src1[0] * blend2 * src2[0];
      dst[1] = blend1 * src1[1] * blend2 * src2[1];
      dst[2] = blend1 * src1[2] * blend2 * src2[2];
      dst[3] = blend1 * src1[3] * blend2 * src2[3];
      dst += 4;
      src1 += 4;
      src2 += 4;
    }
  }
}

