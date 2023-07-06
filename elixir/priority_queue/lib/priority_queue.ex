defmodule PriorityQueue do
  defstruct size: 0, tree: nil

  def new, do: %__MODULE__{size: 0, tree: :gb_trees.empty()}

  def empty?(%__MODULE__{size: 0}), do: true
  def empty?(%__MODULE__{}), do: false

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

  defp push_into_queue(pq = %__MODULE__{size: n, tree: tree}, priority, value, operation)
       when is_integer(priority) do
    tree =
      case :gb_trees.lookup(priority, tree) do
        {:value, queue} ->
          queue =
            case operation do
              :regular -> :queue.in(value, queue)
              :requeue -> :queue.in_r(value, queue)
            end

          :gb_trees.update(priority, queue, tree)

        :none ->
          :gb_trees.insert(priority, :queue.in(value, :queue.new()), tree)
      end

    %{pq | size: n + 1, tree: tree}
  end

  @doc ~S"""
  Appends values into a `PriorityQueue`.

  ## Examples

      iex> queue =
      ...>   PriorityQueue.new()
      ...>   |> PriorityQueue.push(2, 3) # [{2, 3}]
      ...>   |> PriorityQueue.push(1, 1) # [{1, 1}, {2, 3}]
      ...>   |> PriorityQueue.push(3, 5) # [{1, 1}, {2, 3}, {3, 5}]
      ...>   |> PriorityQueue.push_r(2, 2) # [{1, 1}, {2, 2}, {2, 3}, {3, 5}]
      ...>   |> PriorityQueue.push(2, 4) # [{1, 1}, {2, 2}, {2, 3}, {2, 4}, {3, 5}]
      iex> { result, queue } = queue |> PriorityQueue.pop(); result
      { :value, 1, 1 }
      iex> { result, queue } = queue |> PriorityQueue.pop(); result
      { :value, 2, 2 }
      iex> { result, queue } = queue |> PriorityQueue.pop(); result
      { :value, 2, 3 }
      iex> { result, queue } = queue |> PriorityQueue.pop(); result
      { :value, 2, 4 }
      iex> { result, _queue } = queue |> PriorityQueue.pop(); result
      { :value, 3, 5 }
  """
  def push(pq = %__MODULE__{}, priority, value) when is_integer(priority) do
    push_into_queue(pq, priority, value, :regular)
  end

  def push_r(pq = %__MODULE__{}, priority, value) when is_integer(priority) do
    push_into_queue(pq, priority, value, :requeue)
  end

  @doc ~S"""
  Returns the pairs of a `PriorityQueue`.

  ## Examples

      iex> queue =
      ...>   PriorityQueue.new()
      ...>   |> PriorityQueue.push(2, "Lower prio")
      ...>   |> PriorityQueue.push(1, "Higher prio")
      ...>   |> PriorityQueue.push(3, :very_low_prio)
      ...>   |> PriorityQueue.push(2, { :job, "Some other thing in 2" })
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

  def pop(%__MODULE__{size: n, tree: tree}) do
    {priority, queue, tree} = :gb_trees.take_smallest(tree)
    {{:value, value}, queue} = :queue.out(queue)

    tree =
      case :queue.is_empty(queue) do
        false -> :gb_trees.enter(priority, queue, tree)
        true -> tree
      end

    {{:value, priority, value}, %__MODULE__{size: n - 1, tree: tree}}
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
  def keys(%__MODULE__{tree: tree}), do: :gb_trees.keys(tree)

  defp values(pq = %__MODULE__{}, list) do
    case pq |> pop() do
      {:none, _} -> :lists.reverse(list)
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
      {:none, _} -> :lists.reverse(list)
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

      iex> PriorityQueue.new()
      ...> |> PriorityQueue.push(100, 9)
      ...> |> PriorityQueue.push(1, 1)
      ...> |> PriorityQueue.push(2, 5)
      ...> |> PriorityQueue.push(1, 2)
      ...> |> PriorityQueue.push(2, 6)
      ...> |> PriorityQueue.push(1, 3)
      ...> |> PriorityQueue.push(10, 8)
      ...> |> PriorityQueue.push(1, 4)
      ...> |> PriorityQueue.push(2, 7)
      ...> |> PriorityQueue.pairs()

      [
        {1, 1}, {1, 2}, {1, 3}, {1, 4},
        {2, 5}, {2, 6}, {2, 7},
        {10, 8},
        {100, 9}
      ]
  """
  def pairs(pq = %__MODULE__{}) do
    pairs(pq, [])
  end
end
