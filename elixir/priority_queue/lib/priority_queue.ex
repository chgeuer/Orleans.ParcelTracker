defmodule PriorityQueue do
  defstruct size: 0, treex: nil

  def new, do: %__MODULE__{size: 0, treex: Treex.empty()}

  # def empty?(%__MODULE__{treex: tree}), do: tree |> Treex.empty?()
  def empty?(%__MODULE__{size: n}), do: n == 0
  def empty?(_), do: false

  @doc ~S"""
  Returns the size of a `PriorityQueue

  ## Examples

      iex> PriorityQueue.new()
      ...>   |> PriorityQueue.size()
      0

      iex> PriorityQueue.new()
      ...>   |> PriorityQueue.push(1, "Some item")
      ...>   |> PriorityQueue.size()
      1

  """
  def size(%__MODULE__{size: n}), do: n

  def push(pq = %__MODULE__{size: n, treex: tree}, priority, value) when is_integer(priority) do
    tree =
      case tree |> Treex.lookup(priority) do
        {:value, queue} -> tree |> Treex.update!(priority, :queue.in(value, queue))
        :none -> tree |> Treex.insert!(priority, :queue.in(value, :queue.new()))
      end

    %{pq | size: n + 1, treex: tree}
  end

  @doc ~S"""
  Returns the pairs of a `PriorityQueue

  ## Examples

      iex> queue = PriorityQueue.new()
      ...>     |> PriorityQueue.push(2, "Lower prio")
      ...>     |> PriorityQueue.push(1, "Higher prio")
      ...>     |> PriorityQueue.push(3, :very_low_prio)
      ...>     |> PriorityQueue.push(2, { :job, "Some other thing in 2" })
      iex> { result, queue } = queue |> PriorityQueue.pop(); result
      { :value, 1, "Higher prio" }
      iex> { result, queue } = queue |> PriorityQueue.pop(); result
      { :value, 2, "Lower prio" }
      iex> { result, queue } = queue |> PriorityQueue.pop(); result
      { :value, 2, { :job, "Some other thing in 2" } }
      iex> { result, _queue } = queue |> PriorityQueue.pop(); result
      { :value, 3, :very_low_prio }

      iex> { result, _queue } = PriorityQueue.new()
      ...> |> PriorityQueue.pop()
      iex> result
      :none
  """
  def pop(pq = %__MODULE__{size: 0}), do: {:none, pq}

  def pop(%__MODULE__{size: n, treex: tree}) do
    {priority, queue, tree} = tree |> Treex.take_smallest!()
    {{:value, value}, queue} = :queue.out(queue)

    tree =
      case :queue.is_empty(queue) do
        false -> tree |> Treex.enter(priority, queue)
        true -> tree
      end

    {{:value, priority, value}, %__MODULE__{size: n - 1, treex: tree}}
  end

  @doc ~S"""
  Returns the keys of a `PriorityQueue

  ## Examples

      iex> PriorityQueue.new()
      ...> |> PriorityQueue.push(2, "Lower prio")
      ...> |> PriorityQueue.push(1, "Higher prio")
      ...> |> PriorityQueue.keys()

      [ 1, 2 ]

  """
  def keys(%__MODULE__{treex: tree}), do: tree |> Treex.keys()

  defp values(pq = %__MODULE__{}, list) do
    case pq |> pop() do
      {:none, _} -> list |> :lists.reverse()
      {{:value, _priority, value}, pq} -> values(pq, [value | list])
    end
  end

  @doc ~S"""
  Returns the values of a `PriorityQueue

  ## Examples

      iex> PriorityQueue.new()
      ...> |> PriorityQueue.push(2, "Lower prio")
      ...> |> PriorityQueue.push(1, "Higher prio")
      ...> |> PriorityQueue.values()

      [ "Higher prio", "Lower prio" ]

  """
  def values(pq = %__MODULE__{}) do
    values(pq, [])
  end

  defp pairs(pq = %__MODULE__{}, list) do
    case pq |> pop() do
      {:none, _} -> list |> :lists.reverse()
      {{:value, priority, value}, pq} -> pairs(pq, [{priority, value} | list])
    end
  end

  @doc ~S"""
  Returns the pairs of a `PriorityQueue

  ## Examples

      iex> PriorityQueue.new()
      ...> |> PriorityQueue.push(2, "Lower prio")
      ...> |> PriorityQueue.push(1, "Higher prio")
      ...> |> PriorityQueue.push(3, :very_low_prio)
      ...> |> PriorityQueue.push(2, { :job, "Some other thing in 2" })
      ...> |> PriorityQueue.pairs()

      [
        { 1, "Higher prio" },
        { 2, "Lower prio" },
        { 2, { :job, "Some other thing in 2" } },
        { 3, :very_low_prio }
      ]

  """
  def pairs(pq = %__MODULE__{}) do
    pairs(pq, [])
  end
end
