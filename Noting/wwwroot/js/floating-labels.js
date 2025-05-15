document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll("input").forEach((input) => {
        const wrapper = input.closest('.relative');
        const label = wrapper?.querySelector('label');

        const updateLabel = () => {
            if (!label) return;

            if (input.value.trim() !== "") {
                label.classList.add(
                    "top-0",
                    "-translate-y-3",
                    "scale-75",
                    "font-semibold",
                    "tracking-wider",
                );
            } else {
                label.classList.remove(
                    "top-0",
                    "-translate-y-3",
                    "scale-75",
                    "font-semibold",
                    "tracking-wider",
                );
            }
        };

        updateLabel();

        input.addEventListener("input", updateLabel);
    });
});