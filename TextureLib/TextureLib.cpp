// TextureLib.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "TextureLib.hpp"
#include <stdint.h>
#include <math.h>
#include <d3dx9math.h>

#include <xmmintrin.h>

static uint8 shiftAmount[] = {24, 16, 8, 0};

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

int randomSeed = 0x12345;
int tRand() {
  return (randomSeed = (randomSeed * 214013 + 2531011) & RAND_MAX_32) >> 16;
}

double tGaussianRand(double mean, double variance) {
  // Generate a gaussian from the sum of uniformly distributed random numbers
  // (Central Limit Theorem)
  double sum = 0;
  for (int i = 0; i < 100; ++i) {
    sum += randf(-variance, variance);
  }
  return mean + sum / 100;
}

float interpolate(float t) {
  return t*t*t*(t*(t*6-15)+10);
}

enum BlendFunc {
  kBlendAdd,
  kBlendSub,
  kBlendMul,
  kBlendMin,
  kBlendMax
};

void source_solid(int dstTexture, uint32 color) {

  Texture *texture = gTextures[dstTexture];
  float *p = (float *)texture->data;
  float rgba[4];
  for (int i = 0; i < 4; ++i)
    rgba[i] = ((color >> shiftAmount[i]) & 0xff)/ 255.0f;

  __m128 col = _mm_load_ps(rgba);
  int len = texture->len;
  for (int i = 0; i < len; ++i) {
    _mm_store_ps(p, col);
    p += 4;
  }

}

void source_random(int dstTexture, float scale, uint32 seed) {

  Texture *texture = gTextures[dstTexture];
  float *p = (float *)texture->data;

  int tmpSeed = randomSeed;
  randomSeed = seed;

  int len = texture->len;
  for (int i = 0; i < len; ++i) {
    float v = scale * randf(0.0f, 1.0f);
    _mm_store_ps(p, _mm_set_ps1(v));
    p += 4;
  }

  randomSeed = tmpSeed;
}

void source_plasma(int dstTexture, float scale, int monochrome, int startOctave, int endOctave, int seed) {
  Texture *texture = gTextures[dstTexture];
  int tmpSeed = randomSeed;
  randomSeed = seed;

  int height = texture->height;
  int width = texture->width;

  static const int cMaxCorners = 65;
  static int cNumCorners[] = {0, 2, 3, 5, 9, 17, 33, 65};
  static D3DXVECTOR4 corners[cMaxCorners*cMaxCorners];


  for (int i = 0; i < width*height*4; ++i) {
    texture->data[i] = 0;
  }

  for (int octave = startOctave; octave <= endOctave; ++octave) {

    int numCorners = cNumCorners[octave];

    int dx = width / (numCorners - 1);
    int dy = width / (numCorners - 1);

    ASSERT((width % (numCorners - 1)) == 0);
    ASSERT((height % (numCorners - 1)) == 0);

    for (int i = 0; i < numCorners*numCorners; ++i) {
      if (monochrome) {
        _mm_storeu_ps(corners[i], _mm_set_ps1(randf(0.0f, 1.0f)));
      } else {
        for (int j = 0; j < 4; ++j)
          corners[i][j] = randf(0.0f, 1.0f);
      }
    }

    D3DXVECTOR4 *p = (D3DXVECTOR4 *)texture->data;

    for (int i = 0; i < height; ++i) {
      float y = (float)i / height;
      for (int j = 0; j  < width; ++j) {
        float x = (float)j / width;

        // get closest corners
        auto c00 = corners[j/dx+0+(i/dy+0)*numCorners];
        auto c10 = corners[j/dx+0+(i/dy+1)*numCorners];
        auto c01 = corners[j/dx+1+(i/dy+0)*numCorners];
        auto c11 = corners[j/dx+1+(i/dy+1)*numCorners];

        float ttx = (float)j/dx - j/dx;
        float tx = interpolate(ttx);

        float tty = (float)i/dy - i/dy;
        float ty = interpolate(tty);

        auto v0 = lerp(c00, c01, tx);
        auto v1 = lerp(c10, c11, tx);
        auto v = lerp(v0, v1, ty);

        p[0] += scale * v;
        p++;
      }
    }
  }

  randomSeed = tmpSeed;
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

      __m128 m = _mm_set_ps1(v);
      _mm_store_ps(p, m);
      p += 4;

    }
  }

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

struct Circle {
  float x, y, radius;
};

void source_circles(int dstTexture, int amount, float size, float variance, float fade, uint32 innerColor, uint32 outerColor, uint32 seed) {

  const int cMaxCircles = 256;
  static Circle circles[cMaxCircles];
  amount = min(cMaxCircles, amount);

  int seedTmp = randomSeed;
  randomSeed = seed;
  for (int i = 0; i < amount; ++i) {
    circles[i].x = randf(0.0f, 1.0f);
    circles[i].y = randf(0.0f, 1.0f);
    float s = (float)tGaussianRand(0, variance);
    s = size + s;
    circles[i].radius = s;
  }
  randomSeed = seedTmp;
  
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

  int len = dstTexture->len;

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
  D3DXVECTOR4 *dst = (D3DXVECTOR4 *)dstTexture->data;

  Texture *srcTexture = gTextures[srcTextureIdx];
  D3DXVECTOR4 *src = (D3DXVECTOR4 *)srcTexture->data;

  Texture *distortTexture = gTextures[distortTextureIdx];
  D3DXVECTOR4 *distort = (D3DXVECTOR4 *)distortTexture->data;

  int height = dstTexture->height;
  int width = dstTexture->width;

  for (int i = 0; i < height; ++i) {
    float y = (float)i / height;

    for (int j = 0; j  < width; ++j) {
      float x = (float)j / width;

      D3DXVECTOR4 v = *distort;
      float tx = wrap(x + 2*scale*(v.x-0.5f));
      float ty = wrap(y + 2*scale*(v.y-0.5f));

      int ix = (int)tx;
      int iy = (int)ty;

      int tu = (int)(tx*(width-1));
      int tv = (int)(ty*(height-1));

      D3DXVECTOR4 v00 = src[(tv+0)*width+(tu+0)];
      D3DXVECTOR4 v01 = src[(tv+0)*width+(tu+1)];
      D3DXVECTOR4 v10 = src[(tv+1)*width+(tu+0)];
      D3DXVECTOR4 v11 = src[(tv+1)*width+(tu+1)];

      float ttx = tx - ix;
      float tty = ty - iy;

      D3DXVECTOR4 x0 = lerp(v00, v01, ttx);
      D3DXVECTOR4 x1 = lerp(v10, v11, ttx);

      D3DXVECTOR4 xx = lerp(x0, x1, tty);

      *dst = xx;

      distort++;
      dst++;
    }
  }
}
