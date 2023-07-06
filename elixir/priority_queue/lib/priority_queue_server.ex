defmodule PriorityQueueServer do
  use GenServer
  require Logger

  # Client
  def start_link(), do: GenServer.start_link(__MODULE__, PriorityQueue.new())

  def push(pid, priority, value), do: GenServer.cast(pid, {:push, priority, value})

  # :infinity,
  def pop(pid, timeout \\ 5000), do: GenServer.call(pid, :pop, timeout)

  # Server
  @impl true
  def init(state = %PriorityQueue{}), do: {:ok, state}

  @impl true
  def handle_cast({:push, priority, value}, priority_queue) do
    {:noreply, PriorityQueue.push(priority_queue, priority, value)}
  end

  @impl true
  def handle_call(:pop, _from, priority_queue = %PriorityQueue{}) do
    {reply_value, priority_queue} = PriorityQueue.pop(priority_queue)
    {:reply, reply_value, priority_queue}
  end
end
