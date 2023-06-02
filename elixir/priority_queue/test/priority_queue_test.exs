defmodule PriorityQueueTest do
  use ExUnit.Case
  doctest PriorityQueue

  test "queue returns items in priority order" do
    queue =
      PriorityQueue.new()
      |> PriorityQueue.push(2, "Lower prio")
      |> PriorityQueue.push(1, "Higher prio")
      |> PriorityQueue.push(3, :very_low_prio)
      |> PriorityQueue.push(2, {:job, "Some other thing in 2"})

    {val, queue} = queue |> PriorityQueue.pop()
    assert val == {:value, 1, "Higher prio"}

    {val, queue} = queue |> PriorityQueue.pop()
    assert val == {:value, 2, "Lower prio"}

    {val, queue} = queue |> PriorityQueue.pop()
    assert val == {:value, 2, {:job, "Some other thing in 2"}}

    {val, queue} = queue |> PriorityQueue.pop()
    assert val == {:value, 3, :very_low_prio}

    {val, _queue} = queue |> PriorityQueue.pop()
    assert val == :none
  end
end
