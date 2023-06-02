defmodule PriorityQueueServer do
  use GenServer
  require Logger

  defstruct priority_queue: nil, requestors: nil

  # Client

  def start_link(), do: GenServer.start_link(__MODULE__, %__MODULE__{})

  def push(pid, priority, value), do: GenServer.cast(pid, {:push, priority, value})

  # , :infinity
  def pop(pid), do: GenServer.call(pid, :pop)

  # Server

  @impl true
  def init(state = %__MODULE__{}), do: {:ok, state}

  defp add_requestor(requestor), do: requestor |> :queue.in(:queue.new())
  defp add_requestor(nil, requestor), do: add_requestor(requestor)
  defp add_requestor(requestors, requestor), do: requestor |> :queue.in(requestors)

  defp nil_if_empty(requestors) do
    case requestors |> :queue.is_empty() do
      true -> nil
      _ -> requestors
    end
  end

  @impl true
  def handle_cast(
        {:push, priority, value},
        state = %__MODULE__{priority_queue: nil, requestors: nil}
      ),
      do:
        {:noreply,
         %{state | priority_queue: PriorityQueue.new() |> PriorityQueue.push(priority, value)}}

  def handle_cast(
        {:push, priority, value},
        state = %__MODULE__{priority_queue: nil, requestors: requestors}
      ) do
    # A message arrives while people are waiting
    case :queue.out(requestors) do
      {{:value, requestor}, requestors} ->
        GenServer.reply(requestor, {:value, priority, value})
        {:noreply, %{state | requestors: nil_if_empty(requestors)}}

      {:empty, _} ->
        {:noreply,
         %{state | priority_queue: PriorityQueue.new() |> PriorityQueue.push(priority, value)}}
    end
  end

  def handle_cast({:push, priority, value}, state = %__MODULE__{priority_queue: priority_queue}),
    do:
      {:noreply, %{state | priority_queue: priority_queue |> PriorityQueue.push(priority, value)}}

  @impl true
  def handle_call(:pop, from, state = %__MODULE__{priority_queue: nil, requestors: requestors}),
    do: {:noreply, %{state | requestors: add_requestor(requestors, from)}}

  def handle_call(
        :pop,
        from,
        state = %__MODULE__{priority_queue: %PriorityQueue{size: 0}, requestors: requestors}
      ),
      do: {:noreply, %{state | requestors: add_requestor(requestors, from)}}

  def handle_call(
        :pop,
        _from,
        state = %__MODULE__{priority_queue: priority_queue}
      ) do
    {reply_value, priority_queue} = PriorityQueue.pop(priority_queue)
    {:reply, reply_value, %{state | priority_queue: priority_queue}}
  end
end
