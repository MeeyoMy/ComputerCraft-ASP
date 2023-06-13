function update()
    local ws = http.websocket("ws://localhost:5000/ws")
    if ws then
        local storage = peripheral.wrap("back")
        ws.send(os.getComputerLabel() .. "," .. textutils.serialiseJSON(storage.list()))
        os.sleep(2)
        ws.close()
    end
end

while true do
    pcall(update)
    os.sleep(5)
end
