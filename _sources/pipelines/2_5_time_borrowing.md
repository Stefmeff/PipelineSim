# 2.5 Time Borrowing

A key take away from the previous sections is that the clock period must offer enough time to account for the propagation delays of all stages. If the propagation delay of a stage is too large for the chosen clock period, the data will arrive late and will not be sampled correctly. Single stages with large propagation delays will therefore limit the possible clock frequency and can significantly impact the speed of the overall circuit. 

However, there might also be stages within a pipeline that have relatively small propagation delays. Here, early arriving data has to idle and the remaining time of the too large clock period will get wasted. The concept of `time borrowing` tries to compensate these imbalances between the longer and shorter stages of a circuit, by allowing large stages to `borrow' time from shorter stages, were the excess time would simply go unused.

The previous sections already hinted at an inherent characteristic of latches that can be beneficial in this case. In contrast to flip-flops, where data has to setup before the rising clock edge, latches provide up to half a cycle of extra time, since data does not have to setup until the falling edge of the clock. Making use of this additional time for one stage, equally subtracts time reserved for the next stage. This way, larger stages can cut into the excess time that is provided by the shorter stages and get some extra processing time. Using this technique, the clock period can be smaller than the worst-case critical path. The time borrowing property can also add up over multiple cycles, but the overall path must still fit within the available time.

<img src="plots/AbbTimeBorrowing.png" width="70%" style="display:inline-block;">

**Figure 2.5:** *Time Borrowing in latch based systems {cite}`weste2011integrated`*

In latch based systems this works automatically, without requiring any explicit design changes. As seen in Figure 2.5 one path can borrow up to a half a cycle of time from the next path (equally expressed in below). In flip-flop based systems one can achieve the same result by exchanging flip-flops with latches, at paths where time borrowing should be employed  {cite}`Rabaey04, weste2011integrated`.

$$
t_{borrow}  \leq \ \frac{T_{c}}{2} - (t_{setup} + t_{nonoverlap})
$$