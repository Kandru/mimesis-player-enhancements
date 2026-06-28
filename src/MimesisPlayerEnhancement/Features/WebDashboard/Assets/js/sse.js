const Sse = {
  connect(onSnapshot, onMinimap, onError) {
    const source = new EventSource('/api/events');

    source.addEventListener('snapshot', (event) => {
      try {
        onSnapshot(JSON.parse(event.data));
      } catch (err) {
        if (onError) onError(err);
      }
    });

    source.addEventListener('minimap', (event) => {
      try {
        if (onMinimap) onMinimap(JSON.parse(event.data));
      } catch (err) {
        if (onError) onError(err);
      }
    });

    source.onerror = () => {
      if (onError) onError();
    };

    return source;
  },
};
