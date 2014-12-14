if not module then
    function module(modname,...)
    end

    function package.seeall(modul)
    end
    
    require "util"
    util = {}
    util.table = {}
    util.table.deepcopy = table.deepcopy
    util.multiplystripes = multiplystripes
end