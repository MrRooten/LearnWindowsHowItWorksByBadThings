#include "registrylog.h"

AppCompatCache_Win10::AppCompatCache_Win10(BYTE* bytes,size_t length) {
	this->bytes = bytes;
	this->length = length;
	INT32 offsetToRecords = readINT32(bytes, 0);
	

	if (offsetToRecords == 0x34) {
		this->entryNumber = readUINT16(bytes, 0x28);
	}
	else {
		this->entryNumber = readUINT16(bytes, 0x24);
	}

	INT32 index = offsetToRecords;
	while (index < this->length) {
		AppCompatCacheEntry* ce = (AppCompatCacheEntry*)ZMalloc(sizeof(AppCompatCacheEntry));
		ce->sigture = readStringAscii(this->bytes, index, 4);
		if (strcmp(ce->sigture, "10ts") != 0) {
			break;
		}

		index += 4;

		// skip 4 unknown
		index += 4;

		INT32 cedataSize = readINT32(this->bytes, index);
		index += 4;

		ce->pathSize = readINT16(this->bytes, index);
		index += 2;

		ce->path = readString(this->bytes, index, ce->pathSize);
		index += ce->pathSize;

		ce->lastModifyTime = readINT64(this->bytes, index);
		index += 8;

		ce->dataSize = readINT32(this->bytes, index);
		index += 4;
		ce->data = readBYTES(this->bytes, index, ce->dataSize);
		index += ce->dataSize;
		this->entries.push_back(ce);
	}
}

INT32 AppCompatCache_Win10::getEntryNumber() {
	return this->entryNumber;
}
