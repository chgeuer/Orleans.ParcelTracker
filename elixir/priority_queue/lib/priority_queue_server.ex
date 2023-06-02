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

  @impl true
  def handle_cast(
        {:push, priority, value},
        state = %__MODULE__{priority_queue: priority_queue, requestors: requestors}
      ) do
    case {priority_queue, requestors} do
      {nil, nil} ->
        # A first message is pushed
        priority_queue = PriorityQueue.new() |> PriorityQueue.push(priority, value)
        {:noreply, %{state | priority_queue: priority_queue}}

      {nil, requestors} ->
        # A message arrives while people are waiting
        case :queue.out(requestors) do
          {{:value, requestor}, requestors} ->
            GenServer.reply(requestor, {:value, priority, value})

            requestors =
              case requestors |> :queue.is_empty() do
                true -> nil
                _ -> requestors
              end

            state = %{state | requestors: requestors}
            {:noreply, state}

          {:empty, _} ->
            priority_queue = PriorityQueue.new() |> PriorityQueue.push(priority, value)
            state = %{state | priority_queue: priority_queue}
            {:noreply, state}
        end

      {priority_queue, nil} ->
        priority_queue = priority_queue |> PriorityQueue.push(priority, value)
        state = %{state | priority_queue: priority_queue}
        {:noreply, state}
    end
  end

  @impl true
  def handle_call(
        :pop,
        from,
        state = %__MODULE__{priority_queue: priority_queue, requestors: requestors}
      ) do
    case {priority_queue, requestors} do
      {nil, nil} ->
        # Remember the caller for later
        {:noreply, %{state | requestors: :queue.in(from, :queue.new())}}

      {nil, requestors} ->
        # Remember the caller for later
        {:noreply, %{state | requestors: :queue.in(from, requestors)}}

      {priority_queue, requestors} ->
        case PriorityQueue.empty?(priority_queue) do
          true ->
            requestors =
              case requestors do
                nil -> :queue.in(from, :queue.new())
                requestors -> :queue.in(from, requestors)
              end

            state = %{state | requestors: requestors}
            {:noreply, state}

          false ->
            case PriorityQueue.pop(priority_queue) do
              {:none, _} ->
                requestors = :queue.in(from, requestors)
                state = %{state | requestors: requestors}
                {:noreply, state}

              {{:value, priority, value}, priority_queue} ->
                reply_value = {:value, priority, value}
                state = %{state | priority_queue: priority_queue}
                {:reply, reply_value, state}
            end
        end
    end
  end
end
