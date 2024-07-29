"""
usage:
    first copy this script to the folder where the game is installed, or the same folder as BigFile_PC.idx
    then run this script with python3: `python3 unpacker.py`
"""

from pathlib import Path
import re
import struct
import json

workdir = Path(__file__).parent

DATATYPES = {
    "CATALOG": 3,
    "DATA_CONTAINER": 29,
    # 42 VCCI_CHK
    # 63 script file?
    # 65 midi file?
    # 1016 LOCALIZ_
    # 1022 CSNDBNK_
    # 1023 CSNDEVNT
    # 1025 CSNDRTPC
    # 1026 CSNDSWTC
    # 1027 CSNDSTAT
    # 1030 CSNDAXBS
    # 1031 CSNDEVNT
    # 1033 CSNDDATA
    # 2033 ANIMDATA
    # 2072 
    # 2094 FONT____
    # 2132 SHADCOLO
    # 2137 FILETEXT
    # 2142 PHSCMTRL
    # 2172 LOADZONE
    # 2212 EVENTS__
    # 2226 FINALIZE
    "ENGINE_TEXTURE_FILE_RAW": 2229, # ETF_RAW_
    "ENGINE_TEXTURE_FILE_RAW_LOD": 2230, # ETF_RAWL
    # 4077 SINT_SEC
    # 4091 HEADER__
    # 4137 SINT_SEC
    # 4234 GMK_ANIM
    # 4241 GMK_COMG
    # 4254 CSB_CHK_
    # 4288 SCRIPT
    # 4333 SCPTBANK
    # 14000 NAVM____
    # 14003 PTGRAPH_
    # 14014 MOGRAEMU
    "MOTION_GRAPH": 15000, # MG_DATA_%
    # 18002 MENU_RES
    # 19000 NOCHK___
    # 19001 GOGCCHK_
    # 20003 ????
}

def unpack_resources_according_to_idx():
    # if you want to unpack all files, you can uncomment the following line
    # unpack_dir = workdir / "unpacked"
    # unpack_dir.mkdir(exist_ok=True)

    localization_buffers = []

    file_handlers = {
        0: open(workdir / "BigFile_PC.dat", 'rb'),
    }
    for i in range(1, 30):
        file_handlers[i] = open(workdir / f"BigFile_PC.d{i:02}", 'rb')

    with open(workdir / "BigFile_PC.idx", 'rb') as file:
        file.seek(105) # offset copied from https://www.deadray.com/detroit/js/mod.v6.js
        while True:
            entry_data = file.read(28)
            if len(entry_data) < 28: # step copied from https://www.deadray.com/detroit/js/mod.v6.js
                break
            data_type, _, data_id, offset, size, unknown_byte, bigfile_idx = struct.unpack('>IIIIIII', entry_data)
            # the second byte always seems to be 1
            if data_type == 1016:
                file_handlers[bigfile_idx].seek(offset)
                localization_buffers.append(file_handlers[bigfile_idx].read(size))
            # if you want to unpack all files, you can uncomment the following line
            # unpack_type_dir = unpack_dir / str(data_type)
            # unpack_type_dir.mkdir(exist_ok=True)
            # with open(unpack_type_dir / f"{data_id}_{unknown_byte}", 'wb') as fout:
            #     file_handlers[bigfile_idx].seek(offset)
            #     fout.write(file_handlers[bigfile_idx].read(size))
    for handler in file_handlers.values():
        handler.close()
    return localization_buffers

LANGS = [
    'FRE',
    'ENG',
    'GER',
    'ITA',
    'SPA',
    'DUT',
    'POR',
    'SWE',
    'DAN',
    'NOR',
    'FIN',
    'RUS',
    'POL',
    'JPN', # sometimes 'JAP'
    'KOR',
    'CHI',
    'GRE',
    'CZE',
    'HUN',
    'CRO',
    'MEX',
    'BRA',
    'TUR',
    'ARA',
    'SCH',
]

def read_4_bytes_as_len(content_block):
    return struct.unpack('<I', content_block[:4])[0], content_block[4:]

def read_key(key_len, content_block):
    key = content_block[:key_len].decode('ascii')
    content_block = content_block[key_len:]
    return key, content_block

def read_pointer(content_block):
    if content_block[0] == 1:
        pointer1 = content_block[:10]
        content_block = content_block[10:]
    elif content_block[0] == 0:
        pointer1 = content_block[:1]
        content_block = content_block[1:]
    else:
        raise Exception(f'unknown {content_block[0]} {content_block[:16]}')
    return pointer1, content_block

def unpack_loca(localization_buffers):
    out_file = workdir / 'unpacked_localization_text.json'
    out_file_handler = open(out_file, 'w', encoding='utf-8')
    dumped_content = []

    for binary in localization_buffers:
        print(len(binary))
        begin_token = None
        # COM_CONT may defines the jump label of the text, so we need to extract and count it first
        # first 8 byte is 'COM_CONT'
        com_cont_type, com_cont_size = struct.unpack('<II', binary[8:16])
        com_cont_data = binary[16:16+com_cont_size]
        unk_com_cont_header_byte = com_cont_data[:4]
        com_cont_data = com_cont_data[4:]
        com_cont_header_pointers = []
        for com_cont_offset in range(4, com_cont_size, 9):
            com_cont_header_pointers.append(com_cont_data[com_cont_offset:com_cont_offset+5])
        # from the fifth byte, FB 0F 00 00 appears periodically, followed by 5 bytes of something likely a pointer
        begin_token = re.search(b'LOCALIZ_', binary)
        assert begin_token
        info_offset = begin_token.end()
        loca_type, content_size, lang_count = struct.unpack('<III', binary[info_offset:info_offset+12])
        assert loca_type == 6 or loca_type == 5 # 这个byte不是6就是5，不知道是干什么用的
        assert content_size == len(binary) - info_offset - 8
        # 05 type only provide English and French text, 06 is all
        if loca_type == 5:
            # 05 type has a extra padding byte, so we need to skip it and read the lang count
            lang_count = struct.unpack('<I', binary[info_offset+9:info_offset+13])[0]
            content_block = binary[info_offset+13:]
        else:
            content_block = binary[info_offset+12:]
        # every language starts with 01 03 00 00 4-byte identifier, followed by a 4-byte content, such as 00 46 52 45 (decoded to \x00FRE, meaning French)
        # all_occ = re.findall(b'\x01\x03\x00\x00\x00(JAP|FRE|ENG|GER|ITA|SPA|DUT|POR|SWE|DAN|NOR|FIN|RUS|POL|JPN|KOR|CHI|GRE|CZE|HUN|CRO|MEX|BRA|TUR|ARA|SCH)',content_block)

        # if lang_count != len(all_occ):
        #     # be aware of these files, they have a 8-byte 00 00 00 00 04 00 00 00 instead of 01 03 00 00 00 [3-byte language code], which may indicate 'else' or 'other language'
        #     # 1164_0 5 24 23
        #     # 1165_0 5 24 23
        #     # 1591_0 5 24 23
        #     # 568_0 5 24 23
        #     # 569_0 5 24 23
        #     # 572_0 5 24 23
        #     # 580_0 5 24 23
        #     # 582_0 5 24 23
        #     # 619_0 5 24 23
        #     # 630_0 5 24 23
        #     # 643_0 5 24 23
        #     # 644_0 5 24 23
        #     # 730_0 5 24 23
        #     # 731_0 5 24 23
        #     # 885_0 5 24 23
        #     print(filename,unk_06,lang_count,len(all_occ))
        # assert lang_count >= len(all_occ) and lang_count - len(all_occ) <= 1
        if loca_type == 6: # I only care about 06 type
            # print('solving', filename, lang_count)
            while lang_count:
                lang_count -= 1
                lang_specifier = content_block[:4]
                assert lang_specifier == b'\x01\x03\x00\x00'
                # if lang_specifier == b'\x01\x03\x00\x00':
                cur_lang = content_block[5:8].decode('ascii') # skip content_block[4], which is 0x00
                content_block = content_block[8:]
                # else:
                    # cur_lang = 'ELSE'
                    # content_block = content_block[4:]
                while True:
                    if not content_block:
                        break
                    # unk_byte_key_header first appears in a language block is the number of keys that will appear next, and next it should always be 1, we don't really need it
                    unk_byte_key_header, key_len = struct.unpack('<II', content_block[:8])
                    content_block = content_block[8:]
                    if key_len == 0:
                        # print(unk_byte_key_header, cur_lang, 'END')
                        break
                    if len(com_cont_header_pointers) >= key_len and 256 > struct.unpack('<I', content_block[:4])[0]: # multiple keys case
                        # I never see any token longer than 256. If you see one, you may need to change this number
                        # I didn't found a elegant way to solve this, so I just compare it to 256
                        pointers = []
                        keys = []
                        key_lens = []
                        for _ in range(key_len):
                            key_lenN, content_block = read_4_bytes_as_len(content_block)
                            keyN, content_block = read_key(key_lenN, content_block)
                            pointer, content_block = read_pointer(content_block)
                            key_lens.append(key_lenN)
                            keys.append(keyN)
                            pointers.append(pointer)
                        # print(cur_lang, f'[{key_len}KEY]', unk_byte_key_header, key_lens, keys, pointers)
                        if content_block[:4] == b'\x01\x03\x00\x00':
                            break
                        continue

                    key, content_block = read_key(key_len, content_block)
                    text_len = struct.unpack('<I', content_block[:4])[0]
                    text = content_block[4:4+text_len]
                    # print(cur_lang, '[KEY-TEXT]', unk_byte_key_header, key_len, key, text_len, text)
                    # out_file_handler.write(f"{cur_lang},{key},{text.decode('utf-16')}\n")
                    dumped_content.append({
                        'l': cur_lang,
                        'k': key,
                        't': text.decode('utf-16')
                    })
                    content_block = content_block[4+text_len:]
                    if content_block[:4] == b'\x01\x03\x00\x00':
                        break
        # elif loca_type == 5: # If you want to extract 05 type, you may open those file in binary mode and unpack them manually
    json.dump(dumped_content, out_file_handler, ensure_ascii=False)
    out_file_handler.close()

if __name__ == "__main__":
    unpack_loca(unpack_resources_according_to_idx())