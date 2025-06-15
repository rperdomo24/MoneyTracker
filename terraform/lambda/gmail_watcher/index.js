exports.handler = async (event) => {
    console.log("✅ Lambda ejecutada");
    return {
        statusCode: 200,
        body: JSON.stringify({ message: "Hello world from gmail watcher Lambda!" }),
    };
};