defmodule MyTask do
  require Logger

  def launch(name) do
    {:ok, pid} = PriorityQueueServer.start_link()
    Process.register(pid, name)

    name |> PriorityQueueServer.push(1, "Numba 1")
  end

  defp loop(parent, pid) do
    try do
      {:value, priority, value} = pid |> PriorityQueueServer.pop()
      send(parent, {:job, [priority: priority, value: value]})
    catch
      :exit, value ->
        send(parent, {:timeout, value})
    end

    loop(parent, pid)
  end

  def kick_it(pid),
    do:
      Task.async(fn ->
        loop(self(), pid)
      end)

  def receive() do
    receive do
      x -> x
    after
      0 -> :empty
    end
  end
end
