require 'pry-byebug'
def bytes_to_integer bytes
    bytes.unpack("H*").first.to_i(16)
end

def byte4_to_integer_small_end bytes
    bytes.unpack("L").first
end

def byte2_to_integer_small_end bytes
    bytes.unpack("S").first
end

def integer_to_byte2 number 
    [number].pack("S")
end

def integer_to_byte4 number
    [number].pack("L")
end

def integer_to_byte8 number
    [number].pack("Q")
end
def integer_alignment(number,alignment)
    number + (alignment - (number % alignment))
end
class IMAGE_IMPORT_DESCRIPTOR
    def initialize bytes
        @bytes = bytes[0,0x14]
    end

    def orignal_first_thunk
        return byte4_to_integer_small_end @bytes[0,0x4]
    end

    def time_date_stamp
        return byte4_to_integer_small_end @bytes[0x4,0x4]
    end

    def forward_chain
        return byte4_to_integer_small_end @bytes[0x8,0x4]
    end

    def name
        return byte4_to_integer_small_end @bytes[0xc,0x4]
    end

    def first_chunk
        return byte4_to_integer_small_end @bytes[0x10,0x4]
    end

end
class PE32
    attr_accessor :bytes
    class Section 
        attr_accessor :offset
        def initialize bytes
            @bytes = bytes[0,0x28]
        end

        def name
            _ = @bytes[0,0x8]
            _ = _[0.._[0.._.index(/\0/)-1]]
        end

        def virtual_size
            byte4_to_integer_small_end @bytes[0x8,0x4]
        end

        def virtual_address
            byte4_to_integer_small_end @bytes[0xc,0x4]
        end

        def size_of_rawdata
            byte4_to_integer_small_end @bytes[0x10,0x4]
        end

        def pointer_to_rawdata
            byte4_to_integer_small_end @bytes[0x14,0x4]
        end

        def pointer_to_relocations
            byte4_to_integer_small_end @bytes[0x18,0x4]
        end

        def pointer_to_linenumbers
            byte4_to_integer_small_end @bytes[0x1c,0x4]
        end

        def number_of_relocations
            byte4_to_integer_small_end @bytes[0x20,0x2]
        end

        def number_of_linenumbers
            byte4_to_integer_small_end @bytes[0x22,0x2]
        end
        def characteristics
            byte4_to_integer_small_end @bytes[0x24,0x4]
        end
        def get_delta_k
            return self.virtual_address - self.pointer_to_rawdata
        end
    end
    attr_reader :bytes
    def initialize file
        @bytes = IO.read(file)
        
    end

    def pe_header_entry
        if @pe_header != nil
            return @pe_header
        end
        @pe_header = @bytes.index("PE\0\0")
        @pe_header
    end

    def import_table_entry
        if @import_table_offset != nil
            return @import_table_offset
        end
        @import_table_offset = @bytes[pe_header_entry + 0x80,0x4].unpack("L").first
        section_contains_import_table = nil
        self.section_tables.each do |section|
            if @import_table_offset >= section.virtual_address and @import_table_offset <= section.virtual_size + section.virtual_address
                section_contains_import_table = section
                break
            end
        end

        delta_k = section_contains_import_table.virtual_address - section_contains_import_table.pointer_to_rawdata
        @import_table_delta_k = delta_k
        @import_table_offset = @import_table_offset - delta_k
    end

    def section_tables_offset
        pe_header_entry + 0xf8
    end

    def section_tables
        if @sections != nil
            return @sections
        end
        @sections = Array.new
        @number_of_section_tables = byte2_to_integer_small_end @bytes[pe_header_entry + 0x06,0x2]
        start_offset = self.section_tables_offset
        @number_of_section_tables.times do |_|
            _ = Section.new(@bytes[start_offset,0x28])
            _.offset = start_offset
            @sections << _
            start_offset += 0x28
        end
        return @sections
    end

    def inject dll_name,func_name
        @file_alignment = byte4_to_integer_small_end(@bytes[pe_header_entry+0x3c],0x4)
        self.pe_header_entry
        self.import_table_entry
        puts "Expand last section size..."
        last_section_header = self.section_tables.last
        expand_size = integer_alignment(last_section_header.size_of_rawdata+0x100)
        delta_size = expand_size - last_section_header.size_of_rawdata
        @bytes[last_section_header.offset+0x10,0x4] = integer_to_byte4(expand_size)
        @bytes[last_section_header.pointer_to_rawdata,0x0] = delta_size * "\0"
        expand_offset = last_section_header.pointer_to_rawdata + delta_size

        puts "Backup the IID Struture..."
        iid_entry = import_table_entry
        end_of_iid = @bytes[iid_entry..].index("\0"*0x14)
        iid_backup = @bytes[iid_entry..end_of_iid]
        iid_backup += "\0"*0x14
        original_end_of_iid = end_of_iid
        end_of_iid = end_of_iid + 0x14

        puts "Move the backup iid to new expand area..."
        @bytes[expand_offset,iid_backup.size] = iid_backup
        new_iid_entry = original_end_of_iid

        puts "Construct new iid's OriginalFirstThunk,Name,FirstThunk..."
        #@bytes[iid_entry,0x8] = integer_to_byte8(iid_entry+@import_table_delta_k)
        #@bytes[iid_entry+0x8,0x8] = integer_to_byte8(iid_entry+0x8+@import_table_delta_k)
        dll_name = dll_name + "\0"*(integer_alignment(dll_name,0x4)-dll_name.size)
        @bytes[iid_entry+0x10,dll_name.size] = dll_name
        @bytes[iid_entry+0x10+dll_name.size,0x2] = "\0\0"
        func_name = func_name + "\0"*(integer_alignment(func_name,0x4)-func_name.size)
        @bytes[iid_entry+0x12+dll_name.size,func_name.size] = func_name
        @bytes[iid_entry,0x8] = integer_to_byte8(iid_entry+0x12+dll_name.size)
        @bytes[iid_entry+0x8,0x8] = integer_to_byte8(iid_entry,+0x12+dll_name.size)
        
        puts "Fill in new IID Struture..."
        @bytes[new_iid_entry,0x4] = integer_to_byte4(iid_entry+@import_table_delta_k)
        @bytes[new_iid_entry+0xc,0x4] = integer_to_byte4(iid_entry+0x10)
        @bytes[new_iid_entry+0x10,0x4] = integer_to_byte4(iid_entry+0x8)

        puts "Correct PE header information..."
        @bytes[pe_header_entry + 0x80,0x4] = integer_to_byte4(expand_offset+last_section_header.get_delta_k)
        @bytes[pe_header_entry + 0x84,0x4] = integer_to_byte4(iid_backup.size + 0x14)

        @bytes[last_section_header.offset+0x24,0x4] = last_section_header.characteristics | 0x80000000
    end
end


p = PE32.new "./不一样的flag.exe"
p p.import_table_entry


