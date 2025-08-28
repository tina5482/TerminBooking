// wwwroot/js/appointments.js
window.appointmentsCalendar = (function () {
    let cal;

    function timeRange(info) {
        const s = info.event.start;
        const e = info.event.end ?? info.event.start;
        const fmt = (d) =>
            d.toLocaleTimeString("hr-HR", { hour: "2-digit", minute: "2-digit" });
        return `${fmt(s)}–${fmt(e)}`;
    }

    return {
        init() {
            const el = document.getElementById("fc-admin");
            if (!el || typeof FullCalendar === "undefined") return;

            cal = new FullCalendar.Calendar(el, {
                locale: "hr",
                initialView: "timeGridDay",
                slotMinTime: "09:00:00",
                slotMaxTime: "17:00:00",
                allDaySlot: false,
                height: "auto",
                headerToolbar: { left: "prev,next today", center: "title", right: "" },
                eventDidMount(info) {
                    const p = info.event.extendedProps || {};
                    const tip =
                        `${info.event.title}\n${timeRange(info)}` +
                        (p.notes ? `\n${p.notes}` : "");
                    info.el.setAttribute("title", tip);
                },
            });

            cal.render();
        },

        render({ date, events }) {
            if (!cal) return;
            cal.gotoDate(date);
            cal.removeAllEvents();
            cal.addEventSource(events || []);
        },
    };
})();
