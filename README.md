# BaseLib
A library with basic functionality I use in most of my projects (WPF, Web). 

The most amazing class is **RealTimeTracer**, which writes tracing information with at most microsecond
(!) delay, is none blocking and multi threading safe. At least 1000 times faster than tracing in Visual 
Studio.  
Detail documentation: <https://www.codeproject.com/Articles/792532/Debugging-Multithreaded-Code-in-Real-Time>

The most useful class is **Tracer**, which allows tracing of exceptions, errors, warnings and infos with 
little delay. The application can continuously trace what it is doing, because the trace gets overwritten
after some time. But when an exception or error occurs, the trace information can be including, showing
what happened before(!) the problem occurred. Tracer runs efficiently with very little overhead.
Detail documentation: <https://www.codeproject.com/Articles/1142178/ACoreLib-Tracer-Tracing-Done-Right>

BaseLib also contains some Buffers (Ring and FiFo).

**Copyright:** Pubic Domain

