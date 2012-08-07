// TextureLib.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "TextureLib.hpp"
#include <stdint.h>
#include <math.h>
#include <xmmintrin.h>

static const float cPI = 3.1415926f;
static uint8 shiftAmount[] = {24, 16, 8, 0};

static uint8 shiftAmount_argb_to_rgba[] = {16, 8, 0, 24};

#define ELEMS_IN_ARRAY(x) sizeof(x) / sizeof((x)[0])

Texture **gTextures;

// Perlin noise variables
int *gPerm;
Vector2 *gGrad;

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


#define RAND_MAX_32 ((1U << 31) - 1)

int gRandomSeed = 0x12345;
int tRand() {
  return (gRandomSeed = (gRandomSeed * 214013 + 2531011) & RAND_MAX_32) >> 16;
}

float tGaussianRand(float mean, float variance) {
  // Generate a gaussian from the sum of uniformly distributed random numbers
  // (Central Limit Theorem)
  double sum = 0;
  for (int i = 0; i < 100; ++i) {
    sum += randf(-variance, variance);
  }
  return (float)(mean + sum / 100);
}

float interpolate(float t) {
  // 6*t^5-15*t^4+10*t^3
  return t*t*t*(t*(t*6-15)+10);
}

float cos_interpolate(float t) {
  return 1.0f - cosf(t*cPI/2);
}

float bilinear(float *src, int width, int stride, float tx, float ty) {

  // sample corners
  float c00 = src[0];
  float c01 = src[stride];
  float c10 = src[stride*width];
  float c11 = src[stride*(width+1)];

  float v0 = lerp(c00, c01, tx);
  float v1 = lerp(c10, c11, tx);

  return lerp(v0, v1, ty);
}

enum BlendFunc {
  kBlendAdd,
  kBlendSub,
  kBlendMul,
  kBlendMin,
  kBlendMax
};

void source_solid(int dstTexture, uint32 color_argb) {

  const Texture *texture = gTextures[dstTexture];
  float *p = (float *)texture->data;
  float rgba[4];
  for (int i = 0; i < 4; ++i)
    rgba[i] = ((color_argb >> shiftAmount_argb_to_rgba[i]) & 0xff)/ 255.0f;

  __m128 col = _mm_load_ps(rgba);
  int len = texture->width * texture->height;
  for (int i = 0; i < len; ++i) {
    _mm_store_ps(p, col);
    p += 4;
  }

}

void source_random(int dstTexture, float scale, uint32 seed) {

  const Texture *texture = gTextures[dstTexture];
  float *p = (float *)texture->data;

  int tmpSeed = gRandomSeed;
  gRandomSeed = seed;

  int len = texture->width * texture->height;
  for (int i = 0; i < len; ++i) {
    float v = scale * randf(0.0f, 1.0f);
    _mm_store_ps(p, _mm_set_ps1(v));
    p += 4;
  }

  gRandomSeed = tmpSeed;
}

static const int cGridSize[] = {0, (1<<1)+1, (1<<2)+1, (1<<3)+1, (1<<4)+1, (1<<5)+1, (1<<6)+1, (1<<7)+1, (1<<8)+1, (1<<9)+1};
static float corners[4*513*513];

struct PlasmaSettings {
  int pitch;
  int stride;
  int orgSize;
  int orgLogical;
  float roughness;
};

void midpoint_displacement(float *src, PlasmaSettings *settings, int depth, float p00, float p01, float p10, float p11) {

  // p00---top---p01
  //  |  0  |  1  |
  // lft---mid---rgt
  //  |  2  |  3  |
  // p10---btm---p11

  int pitch = settings->pitch;
  int stride = settings->stride;
  int size = settings->orgSize / (1 << depth);
  int logicalSize = settings->orgLogical / (1 << depth);
  float roughness = settings->roughness;

  float top = (p00 + p01) / 2;
  float left = (p00 + p10) / 2;
  float right = (p01 + p11) / 2;
  float bottom = (p10 + p11) / 2;

  float middle = (p00 + p01 + p10 + p11) / 4 + logicalSize / 512.0f * randf(-roughness/2, 0.75f*roughness);

  src[stride*size/2] = top;
  src[pitch*size + stride*size/2] = bottom;

  src[pitch*size/2] = left;
  src[pitch*size/2 + stride*size] = right;

  src[pitch*size/2 + stride*size/2] = middle;

  if (size > 2) {
    int s2 = size / 2;
    int l2 = logicalSize / 2;
    midpoint_displacement(src, settings, depth + 1, p00, top, left, middle);
    midpoint_displacement(src + stride*size/2, settings, depth + 1, top, p01, middle, right);
    midpoint_displacement(src + pitch*size/2, settings, depth + 1, left, middle, p10, bottom);
    midpoint_displacement(src + pitch*size/2 + stride*size/2, settings, depth + 1, middle, right, bottom, p11);
  }
}

void source_plasma(int dstTexture, float scale, int monochrome, int depth, int seed) {
  Texture *texture = gTextures[dstTexture];
  int tmpSeed = gRandomSeed;
  gRandomSeed = seed;

  int height = texture->height;
  int width = texture->width;

  for (int i = 0; i < width*height*4; ++i) {
    texture->data[i] = 0;
  }

  int gridSize = cGridSize[depth];

  ASSERT((width % (gridSize - 1)) == 0);
  ASSERT((height % (gridSize - 1)) == 0);

  int g = gridSize - 1;
  int seedTmp2 = gRandomSeed;
  PlasmaSettings settings;
  settings.orgLogical = width;
  settings.orgSize = g;
  settings.pitch = 4 * gridSize;
  settings.stride = 4;
  settings.roughness = scale;
  for (int k = 0; k < 4; ++k) {
    if (monochrome)
      gRandomSeed = seedTmp2;
    float v = randf(0.0f, 0.3f);
    float v00 = corners[k] = v;
    float v01 = corners[4*g+k] = v;
    float v10 = corners[4*(gridSize*g)+k] = v;
    float v11 = corners[4*(gridSize*g+g)+k] = v;
    midpoint_displacement(corners+k, &settings, 0, v00, v01, v10, v11);
  }

  if (monochrome)
    gRandomSeed = seedTmp2;

  float *p = texture->data;

  int yStep = ((gridSize - 1) << 16) / height;
  int xStep = ((gridSize - 1) << 16) / width;

  for (int i = 0, yCur = 0; i < height; ++i, yCur += yStep) {

    float fracY = (yCur & 0xffff) / 65536.0f;
    fracY = interpolate(fracY);

    for (int j = 0, xCur = 0; j  < width; ++j, xCur += xStep) {

      float fracX = (xCur & 0xffff) / 65536.0f;
      fracX = interpolate(fracX);

      float *tmp = &corners[4*((xCur >> 16) + (yCur >> 16) * gridSize)];
      for (int k = 0; k < 4; ++k)
        *p++ = bilinear(tmp+k, gridSize, 4, fracX, fracY);
    }
  }
  gRandomSeed = tmpSeed;
}

enum SinFunc {
  kFuncSinX,
  kFuncSinY,
  kFuncAdd,
  kFuncMul,
  kSinCos1,
};

void source_sinwaves(int dstTexture, float scale, int func, int numSin, float startAmp, float endAmp, float startPhase, float endPhase, float startFreq, float endFreq) {
  Texture *texture = gTextures[dstTexture];
  float *p = (float *)texture->data;

  int height = texture->height;
  int width = texture->width;

  float denom = float(width - 1);
  float ampDelta = (endAmp - startAmp) / denom;
  float phaseDelta = (endPhase - startPhase) / denom;
  float freqDelta = (endFreq - startFreq) / denom;

  for (int i = 0; i < height; ++i) {
    float y = (float)i / height;
    float amp = startAmp;
    float phase = startPhase;
    float freq = startFreq;
    for (int j = 0; j  < width; ++j) {
      float x = (float)j / width;

      float v = 0;
      for (int k = 0; k < numSin; ++k) {

        float s = scale * amp * sin(phase+x*freq);
        float c = scale * amp * cos(phase+y*freq);

        switch (func) {
        
          case kFuncSinX:
            v += s;
            break;

          case kFuncSinY:
              v += c;
            break;

          case kFuncAdd:
            v += s + c;
            break;

          case kFuncMul: 
            v += s * c;
            break;

          case kSinCos1:
            v += s * sin(c);
            break;

          default: 
            break;
        }
      }
      amp += ampDelta;
      phase += phaseDelta;
      freq += freqDelta;

      for (int k = 0; k < 4; ++k)
        p[k] = v;
      p += 4;
    }
  }
}

float perlin_noise(float x, float y) {
  // grid coordinates
  int gridX = ((int)x) & 0xff;
  int gridY = ((int)y) & 0xff;

  // get corner gradients
  const Vector2 &g00 = gGrad[gPerm[gridX+gPerm[gridY]] % cNumGradients];
  const Vector2 &g01 = gGrad[gPerm[gridX+1+gPerm[gridY]] % cNumGradients];
  const Vector2 &g10 = gGrad[gPerm[gridX+gPerm[gridY+1]] % cNumGradients];
  const Vector2 &g11 = gGrad[gPerm[gridX+1+gPerm[gridY+1]] % cNumGradients];

  // relative pos within cell
  Vector2 pos(x - gridX, y - gridY);

  // dot between gradient and vectors from grid cells to P
  float corners[4] = {
    dot(g00, pos),
    dot(g01, Vector2(pos.x-1, pos.y)),
    dot(g10, Vector2(pos.x, pos.y-1)),
    dot(g11, Vector2(pos.x-1, pos.y-1)),
  };

  // bilinear interpolate
  float fx = interpolate(pos.x);
  float fy = interpolate(pos.y);
  return bilinear(corners, 2, 1, fx, fy);
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
/*
void source_turbulence(int dstTexture, int octaves, float scaleX, float scaleY, float offsetX, float offsetY) {
  Texture *texture = gTextures[dstTexture];
  float *p = (float *)texture->data;

  int height = texture->height;
  int width = texture->width;
  for (int i = 0; i < height; ++i) {
    float y = offsetY + scaleY * i / height;
    for (int j = 0; j  < width; ++j) {
      float x = offsetX + scaleX * j / width;

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
*/
struct Circle {
  float x, y, radius, dummy;
};

static const int cMaxCircles = 256;
static Circle circles[cMaxCircles];

void source_circles(int dstTexture, int amount, float size, float variance, float fade, uint32 innerColor_argb, uint32 outerColor_argb, uint32 seed) {

  amount = min(cMaxCircles, amount);

  int seedTmp = gRandomSeed;
  gRandomSeed = seed;
  for (int i = 0; i < amount; ++i) {
    circles[i].x = randf(0.0f, 1.0f);
    circles[i].y = randf(0.0f, 1.0f);
    float s = (float)tGaussianRand(0, variance);
    s = size + s;
    circles[i].radius = s;
  }
  gRandomSeed = seedTmp;
  
  Texture *texture = gTextures[dstTexture];
  float *p = (float *)texture->data;

  int height = texture->height;
  int width = texture->width;

  float inner[4], outer[4];

  for (int i = 0; i < 4; ++i) {
    inner[i] = ((innerColor_argb >> shiftAmount_argb_to_rgba[i]) & 0xff)/ 255.0f;
    outer[i] = ((outerColor_argb >> shiftAmount_argb_to_rgba[i]) & 0xff)/ 255.0f;
  }

  __m128 innerCol = _mm_loadu_ps(inner);
  __m128 outerCol = _mm_loadu_ps(outer);
  __m128 colorDiff = _mm_sub_ps(outerCol, innerCol);

  for (int i = 0; i < height; ++i) {
    for (int j = 0; j  < width; ++j) {

      float x = j / (float)width;
      float y = i / (float)height;

      _mm_store_ps(p, outerCol);
      const Circle *last_rim = NULL;
      float final_t = 1;

      // Either use the inside color, or interpolate using the min distance
      for (int k = 0; k < amount; ++k) {
        const Circle *cur = &circles[k];
        float dx = cur->x - x;
        float dy = cur->y - y;
        float dist = sqrtf(dx*dx+dy*dy);
        float r = cur->radius;

        if (dist < r) {
          _mm_store_ps(p, innerCol);
          goto INSIDE_CIRCLE;

        } else if (dist < (1 + fade) * r) {

          float s = r;
          float e = (1 + fade) * r;
          float t = (dist - s) / (e - s);
          final_t = min(final_t, t);
          last_rim = cur;
        }
      }

      if (last_rim) {
        __m128 tx = _mm_set_ps1(final_t);
        _mm_store_ps(p, _mm_add_ps(innerCol, _mm_mul_ps(colorDiff, tx)));
      }
INSIDE_CIRCLE:
      p += 4;
    }
  }
}


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

void modifier_invert(int dstTextureIdx, int srcTextureIdx) {

}

void modifier_grayscale(int dstTextureIdx, int srcTextureIdx) {

}

enum DistortChannels {
  kChannelRG,
  kChannelRB,
  kChannelGB,
};

float wrap(float v) {
  while (v < 0)
    v += 1;
  while (v >= 1)
    v -= 1;
  return v;
}

void modifier_map_distort(int dstTextureIdx, int srcTextureIdx, int distortTextureIdx, float scale, int channels) {

  Texture *dstTexture = gTextures[dstTextureIdx];
  float *dst = dstTexture->data;

  Texture *srcTexture = gTextures[srcTextureIdx];
  float *src = srcTexture->data;

  Texture *distortTexture = gTextures[distortTextureIdx];
  float *distort = distortTexture->data;

  int height = dstTexture->height;
  int width = dstTexture->width;

  for (int i = 0; i < height; ++i) {
    float y = (float)i / height;

    for (int j = 0; j  < width; ++j) {
      float x = (float)j / width;

      float tx = wrap(x + 2*scale*(distort[0]-0.5f));
      float ty = wrap(y + 2*scale*(distort[1]-0.5f));

      int ix = (int)tx;
      int iy = (int)ty;

      int tu = (int)(tx*(width-1));
      int tv = (int)(ty*(height-1));

      float ttx = tx - ix;
      float tty = ty - iy;

      float *tmp = &src[4*((tv+0)*width+(tu+0))];
      for (int k = 0; k < 4; ++k) {
        *dst++ = bilinear(tmp, width, 4, ttx, tty);
      }
      distort += 4;

   }
  }
}
