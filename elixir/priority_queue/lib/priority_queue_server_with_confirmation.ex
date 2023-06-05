defmodule PriorityQueueServerWithConfirmation do
  use GenServer
  require Logger

  defstruct pq: nil, in_flight: nil

  # Client
  def start_link(),
    do: GenServer.start_link(__MODULE__, %__MODULE__{pq: PriorityQueue.new(), in_flight: %{}})

  def push(pid, priority, value), do: GenServer.cast(pid, {:push, priority, value})

  # :infinity,
  def pop(pid, timeout \\ 5000), do: GenServer.call(pid, {:pop, timeout})

  def finished(pid, ref), do: GenServer.call(pid, {:finished, ref})

  def length(pid), do: GenServer.call(pid, :length)

  # Server
  @impl true
  def init(state = %__MODULE__{}), do: {:ok, state}

  @impl true
  def handle_cast({:push, priority, value}, state = %__MODULE__{pq: pq}) do
    state = %{state | pq: PriorityQueue.push(pq, priority, value)}
    {:noreply, state}
  end

  @impl true
  def handle_call({:pop, timeout}, _from, state = %__MODULE__{pq: pq, in_flight: in_flight}) do
    {item, new_pq} = pq |> PriorityQueue.pop()

    case item do
      :none ->
        {:reply, :none, state}

      {:value, priority, value} ->
        ref = make_ref()
        in_flight = in_flight |> Map.put(ref, {priority, value})
        state = %{state | pq: new_pq, in_flight: in_flight}

        # After the timeout, check whether the item has been processed
        Process.send_after(self(), {:reschedule, ref}, timeout)

        {:reply, {:value, priority, value, ref}, state}
    end
  end

  @impl true
  def handle_call({:finished, ref}, _from, state = %__MODULE__{in_flight: in_flight}) do
    case in_flight |> Map.pop(ref) do
      {nil, _} ->
        {:reply, :not_found, state}

      {{_priority, _value}, in_flight} ->
        {:reply, :ok, %{state | in_flight: in_flight}}
    end
  end

  def handle_call(:length, _from, state = %__MODULE__{pq: priority_queue}) do
    reply_value = priority_queue |> PriorityQueue.size()
    {:reply, reply_value, state}
  end

  @impl true
  def handle_info({:reschedule, ref}, state = %__MODULE__{pq: pq, in_flight: in_flight}) do
    case in_flight |> Map.pop(ref) do
      {nil, _} ->
        # If the reference isn't there any longer...
        {:noreply, state}

      {{priority, value}, in_flight} ->
        # The reference hasn't been handled in due time, so we re-enqueue it.
        state = %{state | pq: PriorityQueue.push_r(pq, priority, value), in_flight: in_flight}
        {:noreply, state}
    end
  end
end
