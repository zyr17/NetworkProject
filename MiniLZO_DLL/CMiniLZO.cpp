#include <cstdlib>
#include <cstring>
#include <cassert>
#include <Windows.h>
#include "minilzo-2.09/minilzo.h"

#define HEAP_ALLOC(var,size) lzo_align_t __LZO_MMODEL var [ ((size) + (sizeof(lzo_align_t) - 1)) / sizeof(lzo_align_t) ]
static HEAP_ALLOC(wrkmem, LZO1X_1_MEM_COMPRESS);

static void *buf;
static size_t buf_len;

struct lzohdr {
	size_t orig_size;
};


extern "C" __declspec(dllexport) void WINAPI MiniLZO_AllocBuffer(size_t len)
{
	if (len > buf_len) {
		free(buf);
		buf = malloc(len);
		buf_len = len;
	}
}

extern "C" __declspec(dllexport) size_t WINAPI MiniLZO_Compress(void *out, void *in, size_t in_len)
{//return 0;
	assert(in != out);
	MiniLZO_AllocBuffer(in_len + in_len / 16 + 64 + 3);
	lzo_uint out_len;
	int r = lzo1x_1_compress((const unsigned char *) in, in_len, (unsigned char *) buf, &out_len, wrkmem);
	if (r == LZO_E_OK && out_len + sizeof(lzohdr) < in_len) {
		((lzohdr *) out)->orig_size = in_len;
		memcpy((char *) out + sizeof(lzohdr), buf, out_len);
		return out_len + sizeof(lzohdr);
	} else {
		return 0;
	}
}

extern "C" __declspec(dllexport) size_t WINAPI MiniLZO_GetOrigSize(void *in, size_t in_len)
{
	assert(in_len >= sizeof(lzohdr));
	return ((lzohdr *) in)->orig_size;
}

extern "C" __declspec(dllexport) int WINAPI MiniLZO_Decompress(void *out, void *in, size_t in_len)
{
	assert(in != out);
	lzo_uint new_len = MiniLZO_GetOrigSize(in, in_len);
	int r = lzo1x_decompress_safe((unsigned char *) in + sizeof(lzohdr), in_len - sizeof(lzohdr), (unsigned char *) out, &new_len, NULL);
	return r == LZO_E_OK && new_len == MiniLZO_GetOrigSize(in, in_len);
}