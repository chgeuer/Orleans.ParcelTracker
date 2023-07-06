defmodule PriorityQueueServerWithConfirmation do
  use GenServer
  require Logger

  defstruct pq: nil, in_flight: nil

  # Client
  def start_link(),
    do: GenServer.start_link(__MODULE__, %__MODULE__{pq: PriorityQueue.new(), in_flight: Map.new()})

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
    {item, new_pq} = PriorityQueue.pop(pq)

    case item do
      :none ->
        {:reply, :none, state}

      {:value, priority, value} ->
        ref = make_ref()
        in_flight = Map.put(in_flight, ref, {priority, value})
        state = %{state | pq: new_pq, in_flight: in_flight}

        # Schedule an event to check whether the item has been processed, and re-inject if needed
        Process.send_after(self(), {:reschedule, ref}, timeout)

        {:reply, {:value, priority, value, ref}, state}
    end
  end

  @impl true
  def handle_call({:finished, ref}, _from, state = %__MODULE__{in_flight: in_flight}) do
    case Map.pop(in_flight, ref) do
      {nil, _} ->
        {:reply, :not_found, state}

      {{_priority, _value}, in_flight} ->
        {:reply, :ok, %{state | in_flight: in_flight}}
    end
  end

  def handle_call(:length, _from, state = %__MODULE__{pq: pq}) do
    {:reply, PriorityQueue.size(pq), state}
  end

  @impl true
  def handle_info({:reschedule, ref}, state = %__MODULE__{pq: pq, in_flight: in_flight}) do
    #
    # After a given time, check whether the queue item with the given reference is still in-flight, and re-enqueue.
    #
    case Map.pop(in_flight, ref) do
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
